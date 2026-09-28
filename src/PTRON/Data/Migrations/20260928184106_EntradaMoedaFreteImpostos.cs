using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTRON.Data.Migrations
{
    /// <inheritdoc />
    public partial class EntradaMoedaFreteImpostos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cambio",
                table: "EntradasEstoque",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "Frete",
                table: "EntradasEstoque",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Impostos",
                table: "EntradasEstoque",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Moeda",
                table: "EntradasEstoque",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "BRL");

            migrationBuilder.AddColumn<decimal>(
                name: "CustoUnitario",
                table: "EntradaEstoqueItens",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FreteRateado",
                table: "EntradaEstoqueItens",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ImpostoRateado",
                table: "EntradaEstoqueItens",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // Entries created before this migration were priced in reais, with no shipment or taxes.
            migrationBuilder.Sql("UPDATE EntradasEstoque SET Moeda = 'BRL', Cambio = 1;");
            migrationBuilder.Sql("UPDATE EntradaEstoqueItens SET CustoUnitario = PrecoUnitario;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cambio",
                table: "EntradasEstoque");

            migrationBuilder.DropColumn(
                name: "Frete",
                table: "EntradasEstoque");

            migrationBuilder.DropColumn(
                name: "Impostos",
                table: "EntradasEstoque");

            migrationBuilder.DropColumn(
                name: "Moeda",
                table: "EntradasEstoque");

            migrationBuilder.DropColumn(
                name: "CustoUnitario",
                table: "EntradaEstoqueItens");

            migrationBuilder.DropColumn(
                name: "FreteRateado",
                table: "EntradaEstoqueItens");

            migrationBuilder.DropColumn(
                name: "ImpostoRateado",
                table: "EntradaEstoqueItens");
        }
    }
}
