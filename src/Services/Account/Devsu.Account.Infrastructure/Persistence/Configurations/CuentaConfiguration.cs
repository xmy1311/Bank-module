using Devsu.Account.Domain.Entities;
using Devsu.Account.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Account.Infrastructure.Persistence.Configurations;

public sealed class CuentaConfiguration : IEntityTypeConfiguration<Cuenta>
{
    private static readonly DateTime FechaSeed = new(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaSeedCuenta5 = new(2022, 2, 5, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Cuenta> builder)
    {
        builder.HasKey(c => c.CuentaId);
        builder.Property(c => c.CuentaId).ValueGeneratedNever();

        builder.Property(c => c.NumeroCuenta)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(c => c.TipoCuenta)
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(c => c.SaldoInicial).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.SaldoDisponible).HasPrecision(18, 2).IsRequired();

        builder.Property(c => c.Estado).IsRequired();

        builder.Property(c => c.ClienteId)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(c => c.CreadoEn).HasPrecision(3).IsRequired();

  
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        // Colección expuesta como IReadOnlyCollection: EF escribe en el campo privado
        builder.HasMany(c => c.Movimientos)
            .WithOne()
            .HasForeignKey(m => m.CuentaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.Movimientos)
            .HasField("_movimientos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.NumeroCuenta)
            .IsUnique()
            .HasDatabaseName("UX_Cuentas_NumeroCuenta");


        builder.HasIndex(c => c.ClienteId)
            .HasDatabaseName("IX_Cuentas_ClienteId")
            .IncludeProperties(c => new
            {
                c.NumeroCuenta,
                c.TipoCuenta,
                c.SaldoInicial,
                c.SaldoDisponible,
                c.Estado
            });

        builder.ToTable("Cuentas", t =>
        {
            t.HasCheckConstraint("CK_Cuentas_TipoCuenta",
                "[TipoCuenta] IN ('Ahorros','Corriente')");
            t.HasCheckConstraint("CK_Cuentas_SaldoInicial",
                "[SaldoInicial] >= 0");
            t.HasCheckConstraint("CK_Cuentas_SaldoDisponible",
                "[SaldoDisponible] >= 0");
        });

        // Casos de Uso 2 y 3 del enunciado.
        builder.HasData(
            new { CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000001"), NumeroCuenta = "478758", TipoCuenta = TipoCuenta.Ahorros,   SaldoInicial = 2000.00m, SaldoDisponible = 1425.00m, Estado = true, ClienteId = "CLI-0001", CreadoEn = FechaSeed },
            new { CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000002"), NumeroCuenta = "225487", TipoCuenta = TipoCuenta.Corriente, SaldoInicial =  100.00m, SaldoDisponible =  700.00m, Estado = true, ClienteId = "CLI-0002", CreadoEn = FechaSeed },
            new { CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000003"), NumeroCuenta = "495878", TipoCuenta = TipoCuenta.Ahorros,   SaldoInicial =    0.00m, SaldoDisponible =  150.00m, Estado = true, ClienteId = "CLI-0003", CreadoEn = FechaSeed },
            new { CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000004"), NumeroCuenta = "496825", TipoCuenta = TipoCuenta.Ahorros,   SaldoInicial =  540.00m, SaldoDisponible =    0.00m, Estado = true, ClienteId = "CLI-0002", CreadoEn = FechaSeed },
            // Caso de Uso 3: nueva cuenta corriente para Jose Lema, sin movimientos
            new { CuentaId = Guid.Parse("019205a2-0001-7000-8000-000000000005"), NumeroCuenta = "585545", TipoCuenta = TipoCuenta.Corriente, SaldoInicial = 1000.00m, SaldoDisponible = 1000.00m, Estado = true, ClienteId = "CLI-0001", CreadoEn = FechaSeedCuenta5 });
    }
}
