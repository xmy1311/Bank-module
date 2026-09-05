using System.Reflection;
using Devsu.Customer.Application.Exceptions;
using Devsu.Customer.Application.Interfaces;
using Devsu.Customer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Customer.Infrastructure.Persistence;


public sealed class CustomerDbContext : DbContext, IUnitOfWork
{
    public const string SecuenciaClienteId = "SeqClienteId";

    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<Persona> Personas => Set<Persona>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.HasSequence<int>(SecuenciaClienteId).StartsAt(4).IncrementsBy(1);
    }

    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (SqlErrores.EsViolacionDeUnicidad(ex))
        {
            throw new ConflictoUnicidadException(
                "Ya existe un registro con esos datos únicos.", ex);
        }
    }
}
