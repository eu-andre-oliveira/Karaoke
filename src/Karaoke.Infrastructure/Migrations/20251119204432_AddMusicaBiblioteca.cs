using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karaoke.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMusicaBiblioteca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MusicaBibliotecaId",
                table: "Musicas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MusicasBiblioteca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Artista = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UrlStreaming = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    QuantidadeUsos = table.Column<int>(type: "INTEGER", nullable: false),
                    Bloqueada = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoBloqueio = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPorUsuarioId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicasBiblioteca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicasBiblioteca_AspNetUsers_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Musicas_MusicaBibliotecaId",
                table: "Musicas",
                column: "MusicaBibliotecaId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicasBiblioteca_Artista",
                table: "MusicasBiblioteca",
                column: "Artista");

            migrationBuilder.CreateIndex(
                name: "IX_MusicasBiblioteca_CriadoPorUsuarioId",
                table: "MusicasBiblioteca",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicasBiblioteca_Titulo",
                table: "MusicasBiblioteca",
                column: "Titulo");

            migrationBuilder.AddForeignKey(
                name: "FK_Musicas_MusicasBiblioteca_MusicaBibliotecaId",
                table: "Musicas",
                column: "MusicaBibliotecaId",
                principalTable: "MusicasBiblioteca",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Musicas_MusicasBiblioteca_MusicaBibliotecaId",
                table: "Musicas");

            migrationBuilder.DropTable(
                name: "MusicasBiblioteca");

            migrationBuilder.DropIndex(
                name: "IX_Musicas_MusicaBibliotecaId",
                table: "Musicas");

            migrationBuilder.DropColumn(
                name: "MusicaBibliotecaId",
                table: "Musicas");
        }
    }
}
