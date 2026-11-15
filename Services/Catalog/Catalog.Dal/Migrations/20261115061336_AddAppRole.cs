using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddAppRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF DATABASE_PRINCIPAL_ID(N'catalog_app') IS NULL
                    CREATE ROLE [catalog_app];
                """);

            migrationBuilder.Sql(
                "GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [catalog_app];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF DATABASE_PRINCIPAL_ID(N'catalog_app') IS NOT NULL
                    DROP ROLE [catalog_app];
                """);
        }
    }
}
