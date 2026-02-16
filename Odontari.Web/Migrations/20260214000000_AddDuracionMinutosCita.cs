using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odontari.Web.Migrations
{
    /// <inheritdoc />
    [Migration("20260214000000_AddDuracionMinutosCita")]
    public partial class AddDuracionMinutosCita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.Citas') AND name = 'DuracionMinutos'
                )
                BEGIN
                    ALTER TABLE dbo.Citas
                    ADD DuracionMinutos INT NOT NULL CONSTRAINT DF_Citas_DuracionMinutos DEFAULT 30;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.Citas') AND name = 'DuracionMinutos'
                )
                BEGIN
                    ALTER TABLE dbo.Citas DROP CONSTRAINT DF_Citas_DuracionMinutos;
                    ALTER TABLE dbo.Citas DROP COLUMN DuracionMinutos;
                END
            ");
        }
    }
}
