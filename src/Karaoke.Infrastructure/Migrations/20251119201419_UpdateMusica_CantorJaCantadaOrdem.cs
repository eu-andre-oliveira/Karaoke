using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karaoke.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMusica_CantorJaCantadaOrdem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Ano",
                table: "Musicas",
                newName: "Ordem");

            migrationBuilder.AddColumn<string>(
                name: "Cantor",
                table: "Musicas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JaCantada",
                table: "Musicas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cantor",
                table: "Musicas");

            migrationBuilder.DropColumn(
                name: "JaCantada",
                table: "Musicas");

            migrationBuilder.RenameColumn(
                name: "Ordem",
                table: "Musicas",
                newName: "Ano");
        }
    }
}
