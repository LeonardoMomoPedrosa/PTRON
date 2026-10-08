using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTRON.Data.Migrations
{
    /// <inheritdoc />
    public partial class EntradaTresMoedas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Moeda",
                table: "EntradasEstoque",
                newName: "MoedaProdutos");

            migrationBuilder.RenameColumn(
                name: "Cambio",
                table: "EntradasEstoque",
                newName: "CambioProdutos");

            migrationBuilder.AddColumn<decimal>(
                name: "CambioImpostos",
                table: "EntradasEstoque",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MoedaDestino",
                table: "EntradasEstoque",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MoedaFrete",
                table: "EntradasEstoque",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MoedaImpostos",
                table: "EntradasEstoque",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            // Existing entries used one currency for products, shipment and taxes.
            // Copying that currency and rate keeps every stored landed cost unchanged.
            migrationBuilder.Sql("""
                UPDATE EntradasEstoque
                SET MoedaImpostos = MoedaProdutos,
                    MoedaFrete = MoedaProdutos,
                    MoedaDestino = 'BRL',
                    CambioImpostos = CambioProdutos;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CambioImpostos",
                table: "EntradasEstoque");

            migrationBuilder.DropColumn(
                name: "MoedaDestino",
                table: "EntradasEstoque");

            migrationBuilder.DropColumn(
                name: "MoedaFrete",
                table: "EntradasEstoque");

            migrationBuilder.DropColumn(
                name: "MoedaImpostos",
                table: "EntradasEstoque");

            migrationBuilder.RenameColumn(
                name: "MoedaProdutos",
                table: "EntradasEstoque",
                newName: "Moeda");

            migrationBuilder.RenameColumn(
                name: "CambioProdutos",
                table: "EntradasEstoque",
                newName: "Cambio");
        }
    }
}
