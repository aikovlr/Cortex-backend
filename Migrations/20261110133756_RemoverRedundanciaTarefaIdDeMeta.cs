using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoverRedundanciaTarefaIdDeMeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migra dados remanescentes de Meta.TarefaId para MetaTarefa antes da remoção
            migrationBuilder.Sql(@"
                INSERT INTO ""MetaTarefa"" (""MetaId"", ""TarefaId"")
                SELECT m.""Id"", m.""TarefaId""
                FROM ""Meta"" m
                WHERE m.""TarefaId"" > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM ""MetaTarefa"" mt
                      WHERE mt.""MetaId"" = m.""Id"" AND mt.""TarefaId"" = m.""TarefaId""
                  );
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Meta_Tarefa_TarefaId",
                table: "Meta");

            migrationBuilder.DropIndex(
                name: "IX_Meta_TarefaId",
                table: "Meta");

            migrationBuilder.DropColumn(
                name: "TarefaId",
                table: "Meta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TarefaId",
                table: "Meta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Meta_TarefaId",
                table: "Meta",
                column: "TarefaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meta_Tarefa_TarefaId",
                table: "Meta",
                column: "TarefaId",
                principalTable: "Tarefa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
