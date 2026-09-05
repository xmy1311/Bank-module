using System.Data;
using System.Globalization;
using Devsu.Customer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Devsu.Customer.Infrastructure.Persistence;


public sealed class ClienteIdGenerator : IClienteIdGenerator
{
    private const string Prefijo = "CLI-";
    private const string Sql = "SELECT NEXT VALUE FOR dbo.SeqClienteId";

    private readonly CustomerDbContext _context;

    public ClienteIdGenerator(CustomerDbContext context) => _context = context;

 
    public async Task<string> SiguienteAsync(CancellationToken ct)
    {
        var conexion = _context.Database.GetDbConnection();
        var laAbrimosNosotros = conexion.State != ConnectionState.Open;

        if (laAbrimosNosotros)
        {
            await conexion.OpenAsync(ct);
        }

        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = Sql;

            comando.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

            var valor = await comando.ExecuteScalarAsync(ct)
                        ?? throw new InvalidOperationException(
                            "La secuencia dbo.SeqClienteId no devolvió ningún valor.");

            var siguiente = Convert.ToInt32(valor, CultureInfo.InvariantCulture);

            return $"{Prefijo}{siguiente:D4}";
        }
        finally
        {
            if (laAbrimosNosotros)
            {
                await conexion.CloseAsync();
            }
        }
    }
}
