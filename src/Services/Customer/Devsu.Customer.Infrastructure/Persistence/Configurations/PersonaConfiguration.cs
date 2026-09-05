using Devsu.Customer.Domain.Entities;
using Devsu.Customer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Customer.Infrastructure.Persistence.Configurations;


public sealed class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        // TPT: tabla propia para la clase base.

        builder.HasKey(p => p.PersonaId);
        builder.Property(p => p.PersonaId).ValueGeneratedNever();

        builder.Property(p => p.Nombre)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Genero)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(p => p.Edad).IsRequired();

        builder.Property(p => p.Identificacion)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(p => p.Direccion)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Telefono)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(p => p.Identificacion)
            .IsUnique()
            .HasDatabaseName("UX_Personas_Identificacion");

        builder.ToTable("Personas", t =>
        {
            t.HasCheckConstraint("CK_Personas_Genero",
                "[Genero] IN ('Masculino','Femenino','Otro')");
            t.HasCheckConstraint("CK_Personas_Edad",
                "[Edad] >= 0 AND [Edad] <= 150");
            t.HasCheckConstraint("CK_Personas_Nombre",
                "LEN(LTRIM(RTRIM([Nombre]))) > 0");
            t.HasCheckConstraint("CK_Personas_Identificacion",
                "LEN(LTRIM(RTRIM([Identificacion]))) >= 5");
        });

    }
}
