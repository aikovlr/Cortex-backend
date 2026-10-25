using Cortex.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusSugestao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:CanalNotificacao", "email,push,sms")
                .Annotation("Npgsql:Enum:CanalNotificacao.canal_notificacao", "email,push,sms")
                .Annotation("Npgsql:Enum:Cargo", "admin,dono,membro")
                .Annotation("Npgsql:Enum:Cargo.cargo", "dono,admin,membro")
                .Annotation("Npgsql:Enum:Prioridade", "alta,baixa,media,urgente")
                .Annotation("Npgsql:Enum:Prioridade.prioridade", "baixa,media,alta,urgente")
                .Annotation("Npgsql:Enum:Status", "atrasada,concluida,pendente")
                .Annotation("Npgsql:Enum:Status.status", "pendente,atrasada,concluida")
                .Annotation("Npgsql:Enum:StatusSugestao", "aprovada,pendente,rejeitada")
                .Annotation("Npgsql:Enum:StatusSugestao.status_sugestao", "pendente,aprovada,rejeitada")
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

            migrationBuilder.AddColumn<StatusSugestao>(
                name: "Status",
                table: "Sugestao",
                type: "\"StatusSugestao\"",
                nullable: false,
                defaultValue: StatusSugestao.Pendente);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Sugestao");

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
                .OldAnnotation("Npgsql:Enum:CanalNotificacao", "email,push,sms")
                .OldAnnotation("Npgsql:Enum:CanalNotificacao.canal_notificacao", "email,push,sms")
                .OldAnnotation("Npgsql:Enum:Cargo", "admin,dono,membro")
                .OldAnnotation("Npgsql:Enum:Cargo.cargo", "dono,admin,membro")
                .OldAnnotation("Npgsql:Enum:Prioridade", "alta,baixa,media,urgente")
                .OldAnnotation("Npgsql:Enum:Prioridade.prioridade", "baixa,media,alta,urgente")
                .OldAnnotation("Npgsql:Enum:Status", "atrasada,concluida,pendente")
                .OldAnnotation("Npgsql:Enum:Status.status", "pendente,atrasada,concluida")
                .OldAnnotation("Npgsql:Enum:StatusSugestao", "aprovada,pendente,rejeitada")
                .OldAnnotation("Npgsql:Enum:StatusSugestao.status_sugestao", "pendente,aprovada,rejeitada")
                .OldAnnotation("Npgsql:Enum:TipoEntidade", "resposta_tarefa,tarefa,usuario")
                .OldAnnotation("Npgsql:Enum:TipoEntidade.tipo_entidade", "usuario,tarefa,resposta_tarefa");
        }
    }
}
