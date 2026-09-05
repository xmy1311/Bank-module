using System.Reflection;
using Devsu.Account.Application.Exceptions;
using Devsu.Account.Application.Interfaces;
using Devsu.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Account.Infrastructure.Persistence;

public sealed class AccountDbContext : DbContext, IUnitOfWork
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
    {
    }

    public DbSet<Cuenta> Cuentas => Set<Cuenta>();

    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    public DbSet<ClienteReplica> ClientesReplica => Set<ClienteReplica>();

    public DbSet<EventoProcesado> EventosProcesados => Set<EventoProcesado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Traduce la excepción de concurrencia de EF Core a un tipo de la Application,
    /// </summary>
    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictoConcurrenciaException(
                "El registro fue modificado por otra operación concurrente.", ex);
        }
        catch (DbUpdateException ex) when (SqlErrores.EsViolacionDeUnicidad(ex))
        {
            throw new ConflictoUnicidadException(
                "Ya existe un registro con esos datos únicos.", ex);
        }
    }

    void IUnitOfWork.DescartarCambios() => ChangeTracker.Clear();
}
