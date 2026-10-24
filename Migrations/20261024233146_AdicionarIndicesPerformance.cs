using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarIndicesPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tarefa_DataVencimento",
                table: "Tarefa",
                column: "DataVencimento");

            migrationBuilder.CreateIndex(
                name: "IX_Tarefa_Prioridade",
                table: "Tarefa",
                column: "Prioridade");

            migrationBuilder.CreateIndex(
                name: "IX_Tarefa_Status",
                table: "Tarefa",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tarefa_Status_DataVencimento",
                table: "Tarefa",
                columns: new[] { "Status", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsavelTarefa_TarefaId_UsuarioId",
                table: "ResponsavelTarefa",
                columns: new[] { "TarefaId", "UsuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_UsuarioId_Concluida",
                table: "Meta",
                columns: new[] { "UsuarioId", "Concluida" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tarefa_DataVencimento",
                table: "Tarefa");

            migrationBuilder.DropIndex(
                name: "IX_Tarefa_Prioridade",
                table: "Tarefa");

            migrationBuilder.DropIndex(
                name: "IX_Tarefa_Status",
                table: "Tarefa");

            migrationBuilder.DropIndex(
                name: "IX_Tarefa_Status_DataVencimento",
                table: "Tarefa");

            migrationBuilder.DropIndex(
                name: "IX_ResponsavelTarefa_TarefaId_UsuarioId",
                table: "ResponsavelTarefa");

            migrationBuilder.DropIndex(
                name: "IX_Meta_UsuarioId_Concluida",
                table: "Meta");
        }
    }
}
