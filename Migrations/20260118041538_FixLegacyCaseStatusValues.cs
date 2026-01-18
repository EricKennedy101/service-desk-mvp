using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRAServiceRequestPortal.Migrations
{
    /// <inheritdoc />
    public partial class FixLegacyCaseStatusValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Cases"
                SET "Status" = 'Investigating'
                WHERE "Status" = 'In Progress';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
