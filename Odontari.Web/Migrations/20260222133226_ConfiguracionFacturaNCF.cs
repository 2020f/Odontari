using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odontari.Web.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracionFacturaNCF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CondicionesPago",
                table: "Clinicas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionFiscal",
                table: "Clinicas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FormaPagoCredito",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FormaPagoEfectivo",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FormaPagoMixto",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FormaPagoTarjeta",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FormaPagoTransferencia",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ItbisAplicarPorDefecto",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ItbisTasa",
                table: "Clinicas",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Clinicas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MensajeFactura",
                table: "Clinicas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModoFacturacion",
                table: "Clinicas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarFirma",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarQR",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NombreComercial",
                table: "Clinicas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotaLegal",
                table: "Clinicas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PermitirInternaConFiscal",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RNC",
                table: "Clinicas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazonSocial",
                table: "Clinicas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NCFTipos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequiereRNCCliente = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCFTipos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    NumeroInterno = table.Column<int>(type: "int", nullable: false),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    NCFTipoId = table.Column<int>(type: "int", nullable: true),
                    NCF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Itbis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    CitaId = table.Column<int>(type: "int", nullable: true),
                    OrdenCobroId = table.Column<int>(type: "int", nullable: true),
                    FormaPago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreadoAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facturas_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Facturas_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Facturas_NCFTipos_NCFTipoId",
                        column: x => x.NCFTipoId,
                        principalTable: "NCFTipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Facturas_OrdenesCobro_OrdenCobroId",
                        column: x => x.OrdenCobroId,
                        principalTable: "OrdenesCobro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Facturas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NCFRangos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    NCFTipoId = table.Column<int>(type: "int", nullable: false),
                    SeriePrefijo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Desde = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Hasta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Proximo = table.Column<long>(type: "bigint", nullable: false),
                    FechaAutorizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Fuente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreadoAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCFRangos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NCFRangos_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NCFRangos_NCFTipos_NCFTipoId",
                        column: x => x.NCFTipoId,
                        principalTable: "NCFTipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NCFMovimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    NCFTipoId = table.Column<int>(type: "int", nullable: false),
                    NCFGenerado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FacturaId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCFMovimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NCFMovimientos_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NCFMovimientos_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NCFMovimientos_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NCFMovimientos_NCFTipos_NCFTipoId",
                        column: x => x.NCFTipoId,
                        principalTable: "NCFTipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CitaId",
                table: "Facturas",
                column: "CitaId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_ClinicaId",
                table: "Facturas",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_NCFTipoId",
                table: "Facturas",
                column: "NCFTipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_OrdenCobroId",
                table: "Facturas",
                column: "OrdenCobroId",
                unique: true,
                filter: "[OrdenCobroId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_PacienteId",
                table: "Facturas",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_NCFMovimientos_ClinicaId",
                table: "NCFMovimientos",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_NCFMovimientos_FacturaId",
                table: "NCFMovimientos",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_NCFMovimientos_NCFTipoId",
                table: "NCFMovimientos",
                column: "NCFTipoId");

            migrationBuilder.CreateIndex(
                name: "IX_NCFMovimientos_UsuarioId",
                table: "NCFMovimientos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_NCFRangos_ClinicaId",
                table: "NCFRangos",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_NCFRangos_NCFTipoId",
                table: "NCFRangos",
                column: "NCFTipoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NCFMovimientos");

            migrationBuilder.DropTable(
                name: "NCFRangos");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DropTable(
                name: "NCFTipos");

            migrationBuilder.DropColumn(
                name: "CondicionesPago",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "DireccionFiscal",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "FormaPagoCredito",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "FormaPagoEfectivo",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "FormaPagoMixto",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "FormaPagoTarjeta",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "FormaPagoTransferencia",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "ItbisAplicarPorDefecto",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "ItbisTasa",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "MensajeFactura",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "ModoFacturacion",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "MostrarFirma",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "MostrarQR",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "NombreComercial",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "NotaLegal",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "PermitirInternaConFiscal",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "RNC",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "RazonSocial",
                table: "Clinicas");
        }
    }
}
