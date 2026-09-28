using System.ComponentModel.DataAnnotations;
using PTRON.Services;

namespace PTRON.Models;

public class EntradaEstoque
{
    public int Id { get; set; }

    public DateTime Data { get; set; } = DateTime.Now;

    /// <summary>BRL or USD. Product prices, shipment and taxes use this currency.</summary>
    [MaxLength(3)]
    public string Moeda { get; set; } = Moedas.Brl;

    /// <summary>Reais per 1 US$. Always 1 when the entry is in BRL.</summary>
    public decimal Cambio { get; set; } = 1m;

    /// <summary>Shipment in the entry currency.</summary>
    public decimal Frete { get; set; }

    /// <summary>Taxes in the entry currency.</summary>
    public decimal Impostos { get; set; }

    public ICollection<EntradaEstoqueItem> Itens { get; set; } = new List<EntradaEstoqueItem>();

    public decimal TotalProdutos => Itens.Sum(i => i.Qtd * i.PrecoUnitario);

    public decimal FatorCambio => Moeda == Moedas.Usd ? Cambio : 1m;

    public decimal TotalMoeda => TotalProdutos + Frete + Impostos;

    public decimal TotalBrl => TotalMoeda * FatorCambio;
}
