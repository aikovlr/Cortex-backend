using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cortex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarMetaEquipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetaEquipe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MetaId = table.Column<int>(type: "integer", nullable: false),
                    EquipeId = table.Column<int>(type: "integer", nullable: false),
                    DataVinculo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaEquipe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetaEquipe_Equipe_EquipeId",
                        column: x => x.EquipeId,
                        principalTable: "Equipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetaEquipe_Meta_MetaId",
                        column: x => x.MetaId,
                        principalTable: "Meta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetaEquipe_EquipeId",
                table: "MetaEquipe",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaEquipe_MetaId",
                table: "MetaEquipe",
                column: "MetaId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaEquipe_MetaId_EquipeId",
                table: "MetaEquipe",
                columns: new[] { "MetaId", "EquipeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetaEquipe");
        }
    }
}
