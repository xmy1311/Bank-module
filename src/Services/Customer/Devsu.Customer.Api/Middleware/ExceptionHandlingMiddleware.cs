using Devsu.Customer.Application.Exceptions;
using Devsu.Customer.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Customer.Api.Middleware;


public sealed class ExceptionHandlingMiddleware
{
    private static readonly Dictionary<string, int> MapaCodigoHttp = new(StringComparer.Ordinal)
    {
        ["ENTIDAD_NO_ENCONTRADA"] = StatusCodes.Status404NotFound,
        ["CLIENTE_DUPLICADO"] = StatusCodes.Status409Conflict,
        ["CREDENCIALES_INVALIDAS"] = StatusCodes.Status422UnprocessableEntity
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
           
            _logger.LogWarning(ex, "Violación de unicidad detectada por la base de datos.");

            await EscribirAsync(
                context,
                StatusCodes.Status409Conflict,
                "RECURSO_DUPLICADO",
                "Ya existe un registro con esos datos. Verifique la identificación.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflicto de concurrencia.");

            await EscribirAsync(
                context,
                StatusCodes.Status409Conflict,
                "CONFLICTO_CONCURRENCIA",
                "El registro fue modificado por otra operación. Vuelva a intentarlo.");
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

    private static async Task EscribirAsync(
        HttpContext context,
        int status,
        string codigo,
        string mensaje)
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

        await context.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json");
    }
}
