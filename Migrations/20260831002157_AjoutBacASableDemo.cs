using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInterventionApi.Migrations
{
    /// <inheritdoc />
    public partial class AjoutBacASableDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "Organizations");
        }
    }
}
