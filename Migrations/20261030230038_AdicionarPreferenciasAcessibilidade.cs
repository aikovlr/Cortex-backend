using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPreferenciasAcessibilidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AltoContraste",
                table: "PreferenciasNotificacao",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TamanhoFonte",
                table: "PreferenciasNotificacao",
                type: "integer",
                nullable: false,
                defaultValue: 16);

            migrationBuilder.AddColumn<string>(
                name: "Tema",
                table: "PreferenciasNotificacao",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "light");

            migrationBuilder.Sql("CREATE OR REPLACE VIEW \"Preferencias\" AS SELECT * FROM \"PreferenciasNotificacao\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS \"Preferencias\";");

            migrationBuilder.DropColumn(
                name: "AltoContraste",
                table: "PreferenciasNotificacao");

            migrationBuilder.DropColumn(
                name: "TamanhoFonte",
                table: "PreferenciasNotificacao");

            migrationBuilder.DropColumn(
                name: "Tema",
                table: "PreferenciasNotificacao");
        }
    }
}
