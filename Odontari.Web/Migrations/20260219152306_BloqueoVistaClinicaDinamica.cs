using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odontari.Web.Migrations
{
    /// <inheritdoc />
    public partial class BloqueoVistaClinicaDinamica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloqueoVistaClinicaDinamicas",
                columns: table => new
                {
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    VistaKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Bloqueada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueoVistaClinicaDinamicas", x => new { x.ClinicaId, x.VistaKey });
                    table.ForeignKey(
                        name: "FK_BloqueoVistaClinicaDinamicas_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueoVistaClinicaDinamicas");
        }
    }
}
