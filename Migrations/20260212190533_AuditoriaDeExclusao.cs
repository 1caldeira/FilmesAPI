using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmesAPI.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaDeExclusao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioExclusaoId",
                table: "Sessoes",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioExclusaoId",
                table: "Filmes",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioExclusaoId",
                table: "Enderecos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioExclusaoId",
                table: "Cinemas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioExclusaoId",
                table: "Sessoes");

            migrationBuilder.DropColumn(
                name: "UsuarioExclusaoId",
                table: "Filmes");

            migrationBuilder.DropColumn(
                name: "UsuarioExclusaoId",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "UsuarioExclusaoId",
                table: "Cinemas");
        }
    }
}
