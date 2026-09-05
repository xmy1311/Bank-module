using Devsu.Account.Domain.Entities;
using Devsu.Account.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Account.Infrastructure.Persistence.Configurations;

public sealed class MovimientoConfiguration : IEntityTypeConfiguration<Movimiento>
{
    public void Configure(EntityTypeBuilder<Movimiento> builder)
    {
        builder.HasKey(m => m.MovimientoId);
        builder.Property(m => m.MovimientoId).ValueGeneratedNever();

        builder.Property(m => m.Fecha).HasPrecision(3).IsRequired();
        builder.Property(m => m.RegistradoEn).HasPrecision(3).IsRequired();

        builder.Property(m => m.TipoMovimiento)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(m => m.Valor).HasPrecision(18, 2).IsRequired();
        builder.Property(m => m.Saldo).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(m => new { m.CuentaId, m.Fecha })
            .HasDatabaseName("IX_Movimientos_CuentaId_Fecha")
            .IsDescending(false, true)
            .IncludeProperties(m => new { m.TipoMovimiento, m.Valor, m.Saldo });

        builder.ToTable("Movimientos", t =>
        {
            t.HasCheckConstraint("CK_Movimientos_Tipo",
                "[TipoMovimiento] IN ('Deposito','Retiro')");
            t.HasCheckConstraint("CK_Movimientos_ValorNoCero",
                "[Valor] <> 0");
            t.HasCheckConstraint("CK_Movimientos_SaldoNoNegativo",
                "[Saldo] >= 0");

            t.HasCheckConstraint("CK_Movimientos_TipoCoherente",
                "([Valor] > 0 AND [TipoMovimiento] = 'Deposito') OR ([Valor] < 0 AND [TipoMovimiento] = 'Retiro')");
        });

        //casos de uso para pruebas unitarias y de integración
        builder.HasData(
            new { MovimientoId = Guid.Parse("019205a3-0001-7000-8000-000000000001"), CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000004"), Fecha = new DateTime(2022, 2,  8, 10, 15, 0, DateTimeKind.Utc), TipoMovimiento = TipoMovimiento.Retiro,   Valor = -540.00m, Saldo =    0.00m, RegistradoEn = new DateTime(2022, 2,  8, 10, 15, 0, DateTimeKind.Utc) },
            new { MovimientoId = Guid.Parse("019205a3-0001-7000-8000-000000000002"), CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000001"), Fecha = new DateTime(2022, 2,  9,  9, 30, 0, DateTimeKind.Utc), TipoMovimiento = TipoMovimiento.Retiro,   Valor = -575.00m, Saldo = 1425.00m, RegistradoEn = new DateTime(2022, 2,  9,  9, 30, 0, DateTimeKind.Utc) },
            new { MovimientoId = Guid.Parse("019205a3-0001-7000-8000-000000000003"), CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000002"), Fecha = new DateTime(2022, 2, 10, 11, 45, 0, DateTimeKind.Utc), TipoMovimiento = TipoMovimiento.Deposito, Valor =  600.00m, Saldo =  700.00m, RegistradoEn = new DateTime(2022, 2, 10, 11, 45, 0, DateTimeKind.Utc) },
            new { MovimientoId = Guid.Parse("019205a3-0001-7000-8000-000000000004"), CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000003"), Fecha = new DateTime(2022, 2, 11, 14, 20, 0, DateTimeKind.Utc), TipoMovimiento = TipoMovimiento.Deposito, Valor =  150.00m, Saldo =  150.00m, RegistradoEn = new DateTime(2022, 2, 11, 14, 20, 0, DateTimeKind.Utc) });
    }
}
