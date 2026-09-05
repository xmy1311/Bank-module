using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Devsu.Customer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "SeqClienteId",
                startValue: 4L);

            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    PersonaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Genero = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Edad = table.Column<int>(type: "int", nullable: false),
                    Identificacion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Telefono = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.PersonaId);
                    table.CheckConstraint("CK_Personas_Edad", "[Edad] >= 0 AND [Edad] <= 150");
                    table.CheckConstraint("CK_Personas_Genero", "[Genero] IN ('Masculino','Femenino','Otro')");
                    table.CheckConstraint("CK_Personas_Identificacion", "LEN(LTRIM(RTRIM([Identificacion]))) >= 5");
                    table.CheckConstraint("CK_Personas_Nombre", "LEN(LTRIM(RTRIM([Nombre]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    PersonaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    PasswordSalt = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    DesactivadoEn = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.PersonaId);
                    table.CheckConstraint("CK_Clientes_Credenciales", "LEN([PasswordHash]) >= 32 AND LEN([PasswordSalt]) >= 16");
                    table.CheckConstraint("CK_Clientes_Desactivacion", "([Estado] = 1 AND [DesactivadoEn] IS NULL) OR ([Estado] = 0 AND [DesactivadoEn] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Clientes_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "PersonaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Personas",
                columns: new[] { "PersonaId", "Direccion", "Edad", "Genero", "Identificacion", "Nombre", "Telefono" },
                values: new object[,]
                {
                    { new Guid("019205a1-0001-7000-8000-000000000001"), "Otavalo sn y principal", 35, "Masculino", "1712345678", "Jose Lema", "098254785" },
                    { new Guid("019205a1-0001-7000-8000-000000000002"), "Amazonas y NNUU", 29, "Femenino", "0923456789", "Marianela Montalvo", "097548965" },
                    { new Guid("019205a1-0001-7000-8000-000000000003"), "13 junio y Equinoccial", 42, "Masculino", "1804567890", "Juan Osorio", "098874587" }
                });

            migrationBuilder.InsertData(
                table: "Clientes",
                columns: new[] { "PersonaId", "ActualizadoEn", "ClienteId", "CreadoEn", "DesactivadoEn", "Estado", "PasswordHash", "PasswordSalt" },
                values: new object[,]
                {
                    { new Guid("019205a1-0001-7000-8000-000000000001"), null, "CLI-0001", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "x5pyxrT7ryEiPc7V/TXeJajxvCjZRp3HomB8Xytn2J0=", "ej8cjlsgTZeh5sBLPY8hWQ==" },
                    { new Guid("019205a1-0001-7000-8000-000000000002"), null, "CLI-0002", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "2SYFs5GQexixZOfaJCK4HyDWVKMCziJganfyvB2mQvU=", "LptNB8GjSG+1DX4pRqyBNw==" },
                    { new Guid("019205a1-0001-7000-8000-000000000003"), null, "CLI-0003", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "O2pH5ri6pKmukgDnNKyhgpLOouFV1WDq/NUfkpegyYw=", "xI1fGgt+QmOa2BL15wO8ag==" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_Clientes_ClienteId",
                table: "Clientes",
                column: "ClienteId",
                unique: true,
                filter: "[ClienteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Personas_Identificacion",
                table: "Personas",
                column: "Identificacion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Personas");

            migrationBuilder.DropSequence(
                name: "SeqClienteId");
        }
    }
}
