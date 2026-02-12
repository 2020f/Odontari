using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odontari.Web.Migrations
{
    /// <inheritdoc />
    public partial class H05_ExpedienteClinico_OdontogramaHistorial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialClinico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoEvento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CitaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialClinico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialClinico_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistorialClinico_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialClinico_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoriasClinicasSistematicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    AlergiasMedicamentos = table.Column<bool>(type: "bit", nullable: true),
                    AlergiasCuales = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AsmaBronquial = table.Column<bool>(type: "bit", nullable: true),
                    ConvulsionesEpilepsia = table.Column<bool>(type: "bit", nullable: true),
                    Diabetes = table.Column<bool>(type: "bit", nullable: true),
                    EnfermedadesCardiacas = table.Column<bool>(type: "bit", nullable: true),
                    Embarazo = table.Column<bool>(type: "bit", nullable: true),
                    EmbarazoSemanas = table.Column<int>(type: "int", nullable: true),
                    EnfermedadesVenereas = table.Column<bool>(type: "bit", nullable: true),
                    FiebreReumatica = table.Column<bool>(type: "bit", nullable: true),
                    Hepatitis = table.Column<bool>(type: "bit", nullable: true),
                    HepatitisCual = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProblemasNeurologicos = table.Column<bool>(type: "bit", nullable: true),
                    ProblemasRenales = table.Column<bool>(type: "bit", nullable: true),
                    ProblemasSinusales = table.Column<bool>(type: "bit", nullable: true),
                    SangradoExcesivo = table.Column<bool>(type: "bit", nullable: true),
                    TrastornosPsiquiatricos = table.Column<bool>(type: "bit", nullable: true),
                    TrastornosDigestivos = table.Column<bool>(type: "bit", nullable: true),
                    TumoresBenignosMalignos = table.Column<bool>(type: "bit", nullable: true),
                    TumoresCuales = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrastornosRespiratorios = table.Column<bool>(type: "bit", nullable: true),
                    TrastornosRespiratoriosCuales = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriasClinicasSistematicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoriasClinicasSistematicas_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoriasClinicasSistematicas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Odontogramas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    EstadoJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoUsuarioId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Odontogramas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Odontogramas_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Odontogramas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinico_CitaId",
                table: "HistorialClinico",
                column: "CitaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinico_ClinicaId",
                table: "HistorialClinico",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinico_PacienteId",
                table: "HistorialClinico",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriasClinicasSistematicas_ClinicaId",
                table: "HistoriasClinicasSistematicas",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriasClinicasSistematicas_PacienteId",
                table: "HistoriasClinicasSistematicas",
                column: "PacienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Odontogramas_ClinicaId",
                table: "Odontogramas",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Odontogramas_PacienteId",
                table: "Odontogramas",
                column: "PacienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialClinico");

            migrationBuilder.DropTable(
                name: "HistoriasClinicasSistematicas");

            migrationBuilder.DropTable(
                name: "Odontogramas");
        }
    }
}
