using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odontari.Web.Migrations
{
    /// <inheritdoc />
    public partial class PermiteConsentimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PermiteConsentimiento",
                table: "Planes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermiteConsentimiento",
                table: "Planes");
        }
    }
}
