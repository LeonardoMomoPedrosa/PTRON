using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTRON.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserIdioma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Idioma",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "pt-BR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Idioma",
                table: "AspNetUsers");
        }
    }
}
