using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odontari.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoOdontograma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoOdontograma",
                table: "Odontogramas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoOdontograma",
                table: "Odontogramas");
        }
    }
}
