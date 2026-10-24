using Cortex.Entities;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPreferenciasNotificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Anexo_Usuario_Id",
                table: "Anexo");

            migrationBuilder.DropForeignKey(
                name: "FK_Anexo_Usuario_UsuarioId",
                table: "Anexo");

            migrationBuilder.DropIndex(
                name: "IX_Anexo_UsuarioId",
                table: "Anexo");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:CanalNotificacao", "email,push,sms")
                .Annotation("Npgsql:Enum:CanalNotificacao.canal_notificacao", "email,push,sms")
                .Annotation("Npgsql:Enum:Cargo", "admin,dono,membro")
                .Annotation("Npgsql:Enum:Cargo.cargo", "dono,admin,membro")
                .Annotation("Npgsql:Enum:Prioridade", "alta,baixa,media,urgente")
                .Annotation("Npgsql:Enum:Prioridade.prioridade", "baixa,media,alta,urgente")
                .Annotation("Npgsql:Enum:Status", "atrasada,concluida,pendente")
                .Annotation("Npgsql:Enum:Status.status", "pendente,atrasada,concluida")
                .Annotation("Npgsql:Enum:TipoEntidade", "resposta_tarefa,tarefa,usuario")
                .Annotation("Npgsql:Enum:TipoEntidade.tipo_entidade", "usuario,tarefa,resposta_tarefa")
                .OldAnnotation("Npgsql:Enum:Cargo", "admin,dono,membro")
                .OldAnnotation("Npgsql:Enum:Cargo.cargo", "dono,admin,membro")
                .OldAnnotation("Npgsql:Enum:Prioridade", "alta,baixa,media,urgente")
                .OldAnnotation("Npgsql:Enum:Prioridade.prioridade", "baixa,media,alta,urgente")
                .OldAnnotation("Npgsql:Enum:Status", "atrasada,concluida,pendente")
                .OldAnnotation("Npgsql:Enum:Status.status", "pendente,atrasada,concluida")
                .OldAnnotation("Npgsql:Enum:TipoEntidade", "resposta_tarefa,tarefa,usuario")
                .OldAnnotation("Npgsql:Enum:TipoEntidade.tipo_entidade", "usuario,tarefa,resposta_tarefa");

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerificado",
                table: "Usuario",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Anexo",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "PreferenciasNotificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Canal = table.Column<CanalNotificacao>(type: "\"CanalNotificacao\"", nullable: false),
                    AntecedenciaHoras = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    AtivoNovaAtribuicao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AtivoVencimento = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AtivoConclusaoMeta = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AtivoTicket = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasNotificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferenciasNotificacao_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anexo_UsuarioId",
                table: "Anexo",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasNotificacao_UsuarioId_Canal",
                table: "PreferenciasNotificacao",
                columns: new[] { "UsuarioId", "Canal" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Anexo_Usuario_UsuarioId",
                table: "Anexo",
                column: "UsuarioId",
                principalTable: "Usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Anexo_Usuario_UsuarioId",
                table: "Anexo");

            migrationBuilder.DropTable(
                name: "PreferenciasNotificacao");

            migrationBuilder.DropIndex(
                name: "IX_Anexo_UsuarioId",
                table: "Anexo");

            migrationBuilder.DropColumn(
                name: "EmailVerificado",
                table: "Usuario");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:Cargo", "admin,dono,membro")
                .Annotation("Npgsql:Enum:Cargo.cargo", "dono,admin,membro")
                .Annotation("Npgsql:Enum:Prioridade", "alta,baixa,media,urgente")
                .Annotation("Npgsql:Enum:Prioridade.prioridade", "baixa,media,alta,urgente")
                .Annotation("Npgsql:Enum:Status", "atrasada,concluida,pendente")
                .Annotation("Npgsql:Enum:Status.status", "pendente,atrasada,concluida")
                .Annotation("Npgsql:Enum:TipoEntidade", "resposta_tarefa,tarefa,usuario")
                .Annotation("Npgsql:Enum:TipoEntidade.tipo_entidade", "usuario,tarefa,resposta_tarefa")
                .OldAnnotation("Npgsql:Enum:CanalNotificacao", "email,push,sms")
                .OldAnnotation("Npgsql:Enum:CanalNotificacao.canal_notificacao", "email,push,sms")
                .OldAnnotation("Npgsql:Enum:Cargo", "admin,dono,membro")
                .OldAnnotation("Npgsql:Enum:Cargo.cargo", "dono,admin,membro")
                .OldAnnotation("Npgsql:Enum:Prioridade", "alta,baixa,media,urgente")
                .OldAnnotation("Npgsql:Enum:Prioridade.prioridade", "baixa,media,alta,urgente")
                .OldAnnotation("Npgsql:Enum:Status", "atrasada,concluida,pendente")
                .OldAnnotation("Npgsql:Enum:Status.status", "pendente,atrasada,concluida")
                .OldAnnotation("Npgsql:Enum:TipoEntidade", "resposta_tarefa,tarefa,usuario")
                .OldAnnotation("Npgsql:Enum:TipoEntidade.tipo_entidade", "usuario,tarefa,resposta_tarefa");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Anexo",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "IX_Anexo_UsuarioId",
                table: "Anexo",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Anexo_Usuario_Id",
                table: "Anexo",
                column: "Id",
                principalTable: "Usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Anexo_Usuario_UsuarioId",
                table: "Anexo",
                column: "UsuarioId",
                principalTable: "Usuario",
                principalColumn: "Id");
        }
    }
}
