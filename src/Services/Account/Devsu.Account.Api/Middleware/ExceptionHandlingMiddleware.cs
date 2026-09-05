using Devsu.Account.Application.Exceptions;
using Devsu.Account.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Account.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    /// <summary>
    /// Traducción código de negocio -> código HTTP. 
    /// SALDO_NO_DISPONIBLE devuelve 422
    /// conflictos de  concurrencia o de versión devuelve 409
    /// </summary>
    private static readonly Dictionary<string, int> MapaCodigoHttp = new(StringComparer.Ordinal)
    {
        ["ENTIDAD_NO_ENCONTRADA"] = StatusCodes.Status404NotFound,
        ["CUENTA_DUPLICADA"] = StatusCodes.Status409Conflict,
        [SaldoNoDisponibleException.CodigoError] = StatusCodes.Status422UnprocessableEntity,
        ["CUENTA_INACTIVA"] = StatusCodes.Status422UnprocessableEntity,
        ["CLIENTE_INACTIVO"] = StatusCodes.Status422UnprocessableEntity
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _entorno;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment entorno)
    {
        _next = next;
        _logger = logger;
        _entorno = entorno;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (SaldoNoDisponibleException ex)
        {
            // Los importes van al LOG, nunca a la respuesta: revelar el saldo
            // actual en un mensaje de error es una filtración de información.
            _logger.LogWarning(
                "Saldo insuficiente en la cuenta {NumeroCuenta}. Saldo {Saldo}, solicitado {Valor}.",
                ex.NumeroCuenta,
                ex.SaldoActual,
                ex.ValorSolicitado);

            await EscribirAsync(context, StatusCodes.Status422UnprocessableEntity, ex.Codigo, ex.Message);
        }
        catch (DomainException ex)
        {
            var status = MapaCodigoHttp.TryGetValue(ex.Codigo, out var codigo)
                ? codigo
                : StatusCodes.Status422UnprocessableEntity;

            _logger.LogWarning("Regla de negocio {Codigo}: {Mensaje}", ex.Codigo, ex.Message);

            await EscribirAsync(context, status, ex.Codigo, ex.Message);
        }
        catch (ConflictoUnicidadException ex)
        {
            // Carrera entre la comprobación previa y el INSERT: el índice UNIQUE
            // protegió el dato, y aquí se devuelve el código correcto en vez de un 500.
            _logger.LogWarning(ex, "Violación de unicidad detectada por la base de datos.");

            await EscribirAsync(
                context,
                StatusCodes.Status409Conflict,
                "RECURSO_DUPLICADO",
                "Ya existe un registro con esos datos. Verifique el número de cuenta.");
        }
        catch (ConflictoConcurrenciaException ex)
        {
            // Solo llega aquí si se agotaron los reintentos del servicio.
            _logger.LogWarning(ex, "Conflicto de concurrencia no resuelto tras los reintentos.");

            await EscribirAsync(
                context,
                StatusCodes.Status409Conflict,
                "CONFLICTO_CONCURRENCIA",
                "La cuenta está siendo modificada por otra operación. Vuelva a intentarlo.");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Petición cancelada por el cliente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado.");

            await EscribirAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "ERROR_INTERNO",
                _entorno.IsDevelopment() ? ex.Message : "Ocurrió un error inesperado.");
        }
    }

    private static async Task EscribirAsync(HttpContext context, int status, string codigo, string mensaje)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var problema = new ProblemDetails
        {
            Status = status,
            Title = mensaje,
            Type = $"https://httpstatuses.io/{status}",
            Instance = context.Request.Path
        };

        problema.Extensions["code"] = codigo;
        problema.Extensions["correlationId"] =
            context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid) ? cid : null;

        context.Response.StatusCode = status;

        // El contentType va en la llamada: WriteAsJsonAsync sobrescribe cualquier
        // Response.ContentType asignado antes.
        await context.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json");
    }
}
