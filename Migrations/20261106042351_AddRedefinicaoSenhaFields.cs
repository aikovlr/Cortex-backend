using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRedefinicaoSenhaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoRedefinicaoExpiracao",
                table: "Usuario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoRedefinicaoHash",
                table: "Usuario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SenhaAlteradaEm",
                table: "Usuario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TentativasRedefinicao",
                table: "Usuario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSolicitacaoRedefinicaoEm",
                table: "Usuario",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoRedefinicaoExpiracao",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "CodigoRedefinicaoHash",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "SenhaAlteradaEm",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "TentativasRedefinicao",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "UltimaSolicitacaoRedefinicaoEm",
                table: "Usuario");
        }
    }
}
