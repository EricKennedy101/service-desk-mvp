using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRAServiceRequestPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByEmail",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Cases",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "DeletedByEmail",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Cases");
        }
    }
}
