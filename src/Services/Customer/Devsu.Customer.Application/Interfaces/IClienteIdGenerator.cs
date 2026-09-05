namespace Devsu.Customer.Application.Interfaces;

/// <summary>
/// Genera el código de negocio del cliente (CLI-0001, CLI-0002, ...).
/// </summary>
public interface IClienteIdGenerator
{
    Task<string> SiguienteAsync(CancellationToken ct);
}
