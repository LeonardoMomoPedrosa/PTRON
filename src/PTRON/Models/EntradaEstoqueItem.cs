using System.ComponentModel.DataAnnotations;

namespace PTRON.Models;

public class EntradaEstoqueItem
{
    public int Id { get; set; }

    public int EntradaEstoqueId { get; set; }
    public EntradaEstoque? EntradaEstoque { get; set; }

    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Qtd { get; set; }

    [Range(0.0, double.MaxValue, ErrorMessage = "O preço não pode ser negativo.")]
    public decimal PrecoUnitario { get; set; }
}
