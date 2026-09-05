using Devsu.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Account.Infrastructure.Persistence.Configurations;

public sealed class ClienteReplicaConfiguration : IEntityTypeConfiguration<ClienteReplica>
{
    private static readonly DateTime FechaSeed = new(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<ClienteReplica> builder)
    {
        builder.ToTable("ClientesReplica");

        builder.HasKey(c => c.ClienteId);

        builder.Property(c => c.ClienteId).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Identificacion).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(c => c.Estado).IsRequired();
        builder.Property(c => c.ActualizadoEn).HasPrecision(3).IsRequired();

        // Se precarga para que la solución sea demostrable desde el primer arranque,
        builder.HasData(
            new { ClienteId = "CLI-0001", Nombre = "Jose Lema",          Identificacion = "1712345678", Estado = true, ActualizadoEn = FechaSeed },
            new { ClienteId = "CLI-0002", Nombre = "Marianela Montalvo", Identificacion = "0923456789", Estado = true, ActualizadoEn = FechaSeed },
            new { ClienteId = "CLI-0003", Nombre = "Juan Osorio",        Identificacion = "1804567890", Estado = true, ActualizadoEn = FechaSeed });
    }
}

public sealed class EventoProcesadoConfiguration : IEntityTypeConfiguration<EventoProcesado>
{
    public void Configure(EntityTypeBuilder<EventoProcesado> builder)
    {
        builder.ToTable("EventosProcesados");

        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).ValueGeneratedNever();

        builder.Property(e => e.TipoEvento).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(e => e.ProcesadoEn).HasPrecision(3).IsRequired();

        // Soporta la purga periódica del histórico de eventos.
        builder.HasIndex(e => e.ProcesadoEn)
            .HasDatabaseName("IX_EventosProcesados_ProcesadoEn");
    }
}
