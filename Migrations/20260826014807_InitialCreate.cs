using System;
using Cortex.Entities;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:Cargo", "admin,dono,membro")
                .Annotation("Npgsql:Enum:Cargo.cargo", "dono,admin,membro")
                .Annotation("Npgsql:Enum:Prioridade", "alta,baixa,media,urgente")
                .Annotation("Npgsql:Enum:Prioridade.prioridade", "baixa,media,alta,urgente")
                .Annotation("Npgsql:Enum:Status", "atrasada,concluida,pendente")
                .Annotation("Npgsql:Enum:Status.status", "pendente,atrasada,concluida")
                .Annotation("Npgsql:Enum:TipoEntidade", "resposta_tarefa,tarefa,usuario")
                .Annotation("Npgsql:Enum:TipoEntidade.tipo_entidade", "usuario,tarefa,resposta_tarefa");

            migrationBuilder.CreateTable(
                name: "Equipe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MembroEquipe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataEntrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cargo = table.Column<Cargo>(type: "\"Cargo\"", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    EquipeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembroEquipe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembroEquipe_Equipe_EquipeId",
                        column: x => x.EquipeId,
                        principalTable: "Equipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MembroEquipe_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tarefa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Pontuacao = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<Status>(type: "\"Status\"", nullable: false),
                    Prioridade = table.Column<Prioridade>(type: "\"Prioridade\"", nullable: false),
                    CriadorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tarefa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tarefa_Usuario_CriadorId",
                        column: x => x.CriadorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Meta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DataFechamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Concluida = table.Column<bool>(type: "boolean", nullable: true),
                    TarefaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meta_Tarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "Tarefa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Meta_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResponsavelTarefa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    TarefaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponsavelTarefa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsavelTarefa_Tarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "Tarefa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResponsavelTarefa_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RespostaTarefa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataResposta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    TarefaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostaTarefa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespostaTarefa_Tarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "Tarefa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RespostaTarefa_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sugestao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TarefaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sugestao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sugestao_Tarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "Tarefa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sugestao_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketReporte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TarefaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketReporte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketReporte_Tarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "Tarefa",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketReporte_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Anexo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UrlCaminho = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TipoEntidade = table.Column<TipoEntidade>(type: "\"TipoEntidade\"", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    TarefaId = table.Column<int>(type: "integer", nullable: true),
                    RespostaTarefaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anexo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anexo_RespostaTarefa_RespostaTarefaId",
                        column: x => x.RespostaTarefaId,
                        principalTable: "RespostaTarefa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Anexo_Tarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "Tarefa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Anexo_Usuario_Id",
                        column: x => x.Id,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Anexo_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anexo_RespostaTarefaId",
                table: "Anexo",
                column: "RespostaTarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexo_TarefaId",
                table: "Anexo",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexo_UsuarioId",
                table: "Anexo",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MembroEquipe_EquipeId",
                table: "MembroEquipe",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MembroEquipe_UsuarioId",
                table: "MembroEquipe",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_TarefaId",
                table: "Meta",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_UsuarioId",
                table: "Meta",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsavelTarefa_TarefaId",
                table: "ResponsavelTarefa",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsavelTarefa_UsuarioId",
                table: "ResponsavelTarefa",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostaTarefa_TarefaId",
                table: "RespostaTarefa",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostaTarefa_UsuarioId",
                table: "RespostaTarefa",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Sugestao_TarefaId",
                table: "Sugestao",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_Sugestao_UsuarioId",
                table: "Sugestao",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tarefa_CriadorId",
                table: "Tarefa",
                column: "CriadorId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketReporte_TarefaId",
                table: "TicketReporte",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketReporte_UsuarioId",
                table: "TicketReporte",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anexo");

            migrationBuilder.DropTable(
                name: "MembroEquipe");

            migrationBuilder.DropTable(
                name: "Meta");

            migrationBuilder.DropTable(
                name: "ResponsavelTarefa");

            migrationBuilder.DropTable(
                name: "Sugestao");

            migrationBuilder.DropTable(
                name: "TicketReporte");

            migrationBuilder.DropTable(
                name: "RespostaTarefa");

            migrationBuilder.DropTable(
                name: "Equipe");

            migrationBuilder.DropTable(
                name: "Tarefa");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
