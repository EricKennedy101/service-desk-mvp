using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRAServiceRequestPortal.Migrations
{
    /// <inheritdoc />
    public partial class RenameTicketsToCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "Cases");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cases",
                table: "Cases",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Cases",
                table: "Cases");

            migrationBuilder.RenameTable(
                name: "Cases",
                newName: "Tickets");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "Id");
        }
    }
}
