using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Devsu.Account.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientesReplica",
                columns: table => new
                {
                    ClienteId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Identificacion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientesReplica", x => x.ClienteId);
                });

            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroCuenta = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TipoCuenta = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoDisponible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    ClienteId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.CuentaId);
                    table.CheckConstraint("CK_Cuentas_SaldoDisponible", "[SaldoDisponible] >= 0");
                    table.CheckConstraint("CK_Cuentas_SaldoInicial", "[SaldoInicial] >= 0");
                    table.CheckConstraint("CK_Cuentas_TipoCuenta", "[TipoCuenta] IN ('Ahorros','Corriente')");
                });

            migrationBuilder.CreateTable(
                name: "EventosProcesados",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoEvento = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ProcesadoEn = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosProcesados", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    MovimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    TipoMovimiento = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RegistradoEn = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.MovimientoId);
                    table.CheckConstraint("CK_Movimientos_SaldoNoNegativo", "[Saldo] >= 0");
                    table.CheckConstraint("CK_Movimientos_Tipo", "[TipoMovimiento] IN ('Deposito','Retiro')");
                    table.CheckConstraint("CK_Movimientos_TipoCoherente", "([Valor] > 0 AND [TipoMovimiento] = 'Deposito') OR ([Valor] < 0 AND [TipoMovimiento] = 'Retiro')");
                    table.CheckConstraint("CK_Movimientos_ValorNoCero", "[Valor] <> 0");
                    table.ForeignKey(
                        name: "FK_Movimientos_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "CuentaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ClientesReplica",
                columns: new[] { "ClienteId", "ActualizadoEn", "Estado", "Identificacion", "Nombre" },
                values: new object[,]
                {
                    { "CLI-0001", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "1712345678", "Jose Lema" },
                    { "CLI-0002", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "0923456789", "Marianela Montalvo" },
                    { "CLI-0003", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "1804567890", "Juan Osorio" }
                });

            migrationBuilder.InsertData(
                table: "Cuentas",
                columns: new[] { "CuentaId", "ClienteId", "CreadoEn", "Estado", "NumeroCuenta", "SaldoDisponible", "SaldoInicial", "TipoCuenta" },
                values: new object[,]
                {
                    { new Guid("019205a2-0001-7000-8000-000000000001"), "CLI-0001", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "478758", 1425.00m, 2000.00m, "Ahorros" },
                    { new Guid("019205a2-0001-7000-8000-000000000002"), "CLI-0002", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "225487", 700.00m, 100.00m, "Corriente" },
                    { new Guid("019205a2-0001-7000-8000-000000000003"), "CLI-0003", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "495878", 150.00m, 0.00m, "Ahorros" },
                    { new Guid("019205a2-0001-7000-8000-000000000004"), "CLI-0002", new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "496825", 0.00m, 540.00m, "Ahorros" },
                    { new Guid("019205a2-0001-7000-8000-000000000005"), "CLI-0001", new DateTime(2022, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), true, "585545", 1000.00m, 1000.00m, "Corriente" }
                });

            migrationBuilder.InsertData(
                table: "Movimientos",
                columns: new[] { "MovimientoId", "CuentaId", "Fecha", "RegistradoEn", "Saldo", "TipoMovimiento", "Valor" },
                values: new object[,]
                {
                    { new Guid("019205a3-0001-7000-8000-000000000001"), new Guid("019205a2-0001-7000-8000-000000000004"), new DateTime(2022, 2, 8, 10, 15, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 8, 10, 15, 0, 0, DateTimeKind.Utc), 0.00m, "Retiro", -540.00m },
                    { new Guid("019205a3-0001-7000-8000-000000000002"), new Guid("019205a2-0001-7000-8000-000000000001"), new DateTime(2022, 2, 9, 9, 30, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 9, 9, 30, 0, 0, DateTimeKind.Utc), 1425.00m, "Retiro", -575.00m },
                    { new Guid("019205a3-0001-7000-8000-000000000003"), new Guid("019205a2-0001-7000-8000-000000000002"), new DateTime(2022, 2, 10, 11, 45, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 10, 11, 45, 0, 0, DateTimeKind.Utc), 700.00m, "Deposito", 600.00m },
                    { new Guid("019205a3-0001-7000-8000-000000000004"), new Guid("019205a2-0001-7000-8000-000000000003"), new DateTime(2022, 2, 11, 14, 20, 0, 0, DateTimeKind.Utc), new DateTime(2022, 2, 11, 14, 20, 0, 0, DateTimeKind.Utc), 150.00m, "Deposito", 150.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_ClienteId",
                table: "Cuentas",
                column: "ClienteId")
                .Annotation("SqlServer:Include", new[] { "NumeroCuenta", "TipoCuenta", "SaldoInicial", "SaldoDisponible", "Estado" });

            migrationBuilder.CreateIndex(
                name: "UX_Cuentas_NumeroCuenta",
                table: "Cuentas",
                column: "NumeroCuenta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosProcesados_ProcesadoEn",
                table: "EventosProcesados",
                column: "ProcesadoEn");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_CuentaId_Fecha",
                table: "Movimientos",
                columns: new[] { "CuentaId", "Fecha" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "TipoMovimiento", "Valor", "Saldo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientesReplica");

            migrationBuilder.DropTable(
                name: "EventosProcesados");

            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "Cuentas");
        }
    }
}
