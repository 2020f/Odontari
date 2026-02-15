using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odontari.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDuracionMinutosCita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuracionMinutos",
                table: "Citas",
                type: "int",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuracionMinutos",
                table: "Citas");
        }
    }
}
