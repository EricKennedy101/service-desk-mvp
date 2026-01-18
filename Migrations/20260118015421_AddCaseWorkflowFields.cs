using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FRAServiceRequestPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToEmail",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstResponseAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Cases",
                type: "text[]",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaseId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    FieldName = table.Column<string>(type: "text", nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ActorEmail = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseEvents");

            migrationBuilder.DropColumn(
                name: "AssignedToEmail",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "FirstResponseAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Cases");
        }
    }
}
