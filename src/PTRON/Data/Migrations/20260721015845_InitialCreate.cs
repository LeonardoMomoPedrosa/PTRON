using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTRON.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntradasEstoque",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradasEstoque", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    FotoPath = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposInsumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposInsumo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    DescricaoAdicional = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustoTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_Equipamentos_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Insumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TipoInsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Valor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Potencia = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Voltagem = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Saldo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    FotoPath = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Insumos_TiposInsumo_TipoInsumoId",
                        column: x => x.TipoInsumoId,
                        principalTable: "TiposInsumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntradaEstoqueItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntradaEstoqueId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Qtd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradaEstoqueItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntradaEstoqueItens_EntradasEstoque_EntradaEstoqueId",
                        column: x => x.EntradaEstoqueId,
                        principalTable: "EntradasEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntradaEstoqueItens_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipamentoInsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Qtd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipamentoInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipamentoInsumos_Equipamentos_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipamentoInsumos_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProdutoInsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Qtd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutoInsumos_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProdutoInsumos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntradaEstoqueItens_EntradaEstoqueId",
                table: "EntradaEstoqueItens",
                column: "EntradaEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_EntradaEstoqueItens_InsumoId",
                table: "EntradaEstoqueItens",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentoInsumos_EquipamentoId",
                table: "EquipamentoInsumos",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipamentoInsumos_InsumoId",
                table: "EquipamentoInsumos",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_TipoInsumoId",
                table: "Insumos",
                column: "TipoInsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoInsumos_InsumoId",
                table: "ProdutoInsumos",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoInsumos_ProdutoId",
                table: "ProdutoInsumos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_EquipamentoId",
                table: "Produtos",
                column: "EquipamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntradaEstoqueItens");

            migrationBuilder.DropTable(
                name: "EquipamentoInsumos");

            migrationBuilder.DropTable(
                name: "ProdutoInsumos");

            migrationBuilder.DropTable(
                name: "EntradasEstoque");

            migrationBuilder.DropTable(
                name: "Insumos");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "TiposInsumo");

            migrationBuilder.DropTable(
                name: "Equipamentos");
        }
    }
}
