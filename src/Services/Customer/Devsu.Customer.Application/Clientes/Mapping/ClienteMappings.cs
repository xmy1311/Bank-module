using Devsu.Customer.Application.Clientes.Dtos;
using Devsu.Customer.Domain.Entities;

namespace Devsu.Customer.Application.Clientes.Mapping;

/// <summary>
/// Mapeo manual. Sin AutoMapper: con ocho DTOs, la configuración por convención
/// esconde más de lo que ahorra y falla en runtime, no en compilación.
/// </summary>
public static class ClienteMappings
{
    public static ClienteResponse ToResponse(this Cliente cliente) => new()
    {
        ClienteId = cliente.ClienteId,
        Nombre = cliente.Nombre,
        Genero = cliente.Genero.ToString(),
        Edad = cliente.Edad,
        Identificacion = cliente.Identificacion,
        Direccion = cliente.Direccion,
        Telefono = cliente.Telefono,
        Estado = cliente.Estado,
        CreadoEn = cliente.CreadoEn,
        ActualizadoEn = cliente.ActualizadoEn
    };
}
