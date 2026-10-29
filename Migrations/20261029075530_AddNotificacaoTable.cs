using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificacaoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Lida = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Destaque = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TarefaId = table.Column<int>(type: "integer", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacao_Tarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "Tarefa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notificacao_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_TarefaId",
                table: "Notificacao",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_UsuarioId_DataCriacao",
                table: "Notificacao",
                columns: new[] { "UsuarioId", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_UsuarioId_Lida",
                table: "Notificacao",
                columns: new[] { "UsuarioId", "Lida" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificacao");
        }
    }
}
