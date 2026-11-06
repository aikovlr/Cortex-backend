using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNovosTiposNotificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AtivoSugestao",
                table: "PreferenciasNotificacao",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AtivoEquipe",
                table: "PreferenciasNotificacao",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AtivoGeral",
                table: "PreferenciasNotificacao",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtivoSugestao",
                table: "PreferenciasNotificacao");

            migrationBuilder.DropColumn(
                name: "AtivoEquipe",
                table: "PreferenciasNotificacao");

            migrationBuilder.DropColumn(
                name: "AtivoGeral",
                table: "PreferenciasNotificacao");
        }
    }
}

