using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTRON.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows belong to the reserved Leo account (Id = "leo").
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "TiposInsumo",
                type: "TEXT",
                nullable: false,
                defaultValue: "leo");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Produtos",
                type: "TEXT",
                nullable: false,
                defaultValue: "leo");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Insumos",
                type: "TEXT",
                nullable: false,
                defaultValue: "leo");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Equipamentos",
                type: "TEXT",
                nullable: false,
                defaultValue: "leo");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "EntradasEstoque",
                type: "TEXT",
                nullable: false,
                defaultValue: "leo");

            migrationBuilder.CreateIndex(
                name: "IX_TiposInsumo_UserId_Nome",
                table: "TiposInsumo",
                columns: new[] { "UserId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_UserId",
                table: "Produtos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_UserId",
                table: "Insumos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_UserId",
                table: "Equipamentos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EntradasEstoque_UserId",
                table: "EntradasEstoque",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_EntradasEstoque_AspNetUsers_UserId",
                table: "EntradasEstoque",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipamentos_AspNetUsers_UserId",
                table: "Equipamentos",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Insumos_AspNetUsers_UserId",
                table: "Insumos",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_AspNetUsers_UserId",
                table: "Produtos",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TiposInsumo_AspNetUsers_UserId",
                table: "TiposInsumo",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntradasEstoque_AspNetUsers_UserId",
                table: "EntradasEstoque");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipamentos_AspNetUsers_UserId",
                table: "Equipamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Insumos_AspNetUsers_UserId",
                table: "Insumos");

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_AspNetUsers_UserId",
                table: "Produtos");

            migrationBuilder.DropForeignKey(
                name: "FK_TiposInsumo_AspNetUsers_UserId",
                table: "TiposInsumo");

            migrationBuilder.DropIndex(
                name: "IX_TiposInsumo_UserId_Nome",
                table: "TiposInsumo");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_UserId",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_Insumos_UserId",
                table: "Insumos");

            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_UserId",
                table: "Equipamentos");

            migrationBuilder.DropIndex(
                name: "IX_EntradasEstoque_UserId",
                table: "EntradasEstoque");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TiposInsumo");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EntradasEstoque");
        }
    }
}
