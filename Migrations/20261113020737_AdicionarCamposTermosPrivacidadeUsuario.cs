using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamposTermosPrivacidadeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceitouTermosPrivacidade",
                table: "Usuario",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataAceiteTermos",
                table: "Usuario",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AceitouTermosPrivacidade",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "DataAceiteTermos",
                table: "Usuario");
        }
    }
}
