using Devsu.Customer.Domain.Entities;
using Devsu.Customer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Customer.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{

    private static readonly DateTime FechaSeed = new(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
     
        builder.Property(c => c.ClienteId)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(c => c.PasswordHash)
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(c => c.PasswordSalt)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(c => c.Estado).IsRequired();
        builder.Property(c => c.CreadoEn).HasPrecision(3).IsRequired();
        builder.Property(c => c.ActualizadoEn).HasPrecision(3);
        builder.Property(c => c.DesactivadoEn).HasPrecision(3);

        builder.HasIndex(c => c.ClienteId)
            .IsUnique()
            .HasDatabaseName("UX_Clientes_ClienteId");

        builder.ToTable("Clientes", t =>
        {
            t.HasCheckConstraint("CK_Clientes_Desactivacion",
                "([Estado] = 1 AND [DesactivadoEn] IS NULL) OR ([Estado] = 0 AND [DesactivadoEn] IS NOT NULL)");

            t.HasCheckConstraint("CK_Clientes_Credenciales",
                "LEN([PasswordHash]) >= 32 AND LEN([PasswordSalt]) >= 16");
        });

        // Seed Completo
        builder.HasData(
            new
            {
                PersonaId = Guid.Parse("019205a1-0001-7000-8000-000000000001"),
                Nombre = "Jose Lema",
                Genero = Genero.Masculino,
                Edad = 35,
                Identificacion = "1712345678",
                Direccion = "Otavalo sn y principal",
                Telefono = "098254785",
                ClienteId = "CLI-0001",
                PasswordHash = "x5pyxrT7ryEiPc7V/TXeJajxvCjZRp3HomB8Xytn2J0=",
                PasswordSalt = "ej8cjlsgTZeh5sBLPY8hWQ==",
                Estado = true,
                CreadoEn = FechaSeed
            },
            new
            {
                PersonaId = Guid.Parse("019205a1-0001-7000-8000-000000000002"),
                Nombre = "Marianela Montalvo",
                Genero = Genero.Femenino,
                Edad = 29,
                Identificacion = "0923456789",
                Direccion = "Amazonas y NNUU",
                Telefono = "097548965",
                ClienteId = "CLI-0002",
                PasswordHash = "2SYFs5GQexixZOfaJCK4HyDWVKMCziJganfyvB2mQvU=",
                PasswordSalt = "LptNB8GjSG+1DX4pRqyBNw==",
                Estado = true,
                CreadoEn = FechaSeed
            },
            new
            {
                PersonaId = Guid.Parse("019205a1-0001-7000-8000-000000000003"),
                Nombre = "Juan Osorio",
                Genero = Genero.Masculino,
                Edad = 42,
                Identificacion = "1804567890",
                Direccion = "13 junio y Equinoccial",
                Telefono = "098874587",
                ClienteId = "CLI-0003",
                PasswordHash = "O2pH5ri6pKmukgDnNKyhgpLOouFV1WDq/NUfkpegyYw=",
                PasswordSalt = "xI1fGgt+QmOa2BL15wO8ag==",
                Estado = true,
                CreadoEn = FechaSeed
            });
    }
}
