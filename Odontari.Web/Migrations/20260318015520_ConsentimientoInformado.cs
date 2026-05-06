using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odontari.Web.Migrations
{
    /// <inheritdoc />
    public partial class ConsentimientoInformado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantillasConsentimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TextoBase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsObligatorio = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasConsentimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillasConsentimiento_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsentimientosGenerados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    PlantillaId = table.Column<int>(type: "int", nullable: false),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    CitaId = table.Column<int>(type: "int", nullable: false),
                    ProcedimientoRealizadoId = table.Column<int>(type: "int", nullable: true),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TextoFinal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionPlantilla = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFirma = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirmaDigitalBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreFirmante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentimientosGenerados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentimientosGenerados_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsentimientosGenerados_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsentimientosGenerados_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsentimientosGenerados_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsentimientosGenerados_PlantillasConsentimiento_PlantillaId",
                        column: x => x.PlantillaId,
                        principalTable: "PlantillasConsentimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsentimientosGenerados_ProcedimientosRealizados_ProcedimientoRealizadoId",
                        column: x => x.ProcedimientoRealizadoId,
                        principalTable: "ProcedimientosRealizados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimientosGenerados_CitaId",
                table: "ConsentimientosGenerados",
                column: "CitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimientosGenerados_ClinicaId",
                table: "ConsentimientosGenerados",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimientosGenerados_DoctorId",
                table: "ConsentimientosGenerados",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimientosGenerados_PacienteId",
                table: "ConsentimientosGenerados",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimientosGenerados_PlantillaId",
                table: "ConsentimientosGenerados",
                column: "PlantillaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimientosGenerados_ProcedimientoRealizadoId",
                table: "ConsentimientosGenerados",
                column: "ProcedimientoRealizadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasConsentimiento_ClinicaId",
                table: "PlantillasConsentimiento",
                column: "ClinicaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentimientosGenerados");

            migrationBuilder.DropTable(
                name: "PlantillasConsentimiento");
        }
    }
}
