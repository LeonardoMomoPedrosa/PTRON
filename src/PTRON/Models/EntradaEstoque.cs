using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PTRON.Services;

namespace PTRON.Models;

public class EntradaEstoque : IUserOwned
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime Data { get; set; } = DateTime.Now;

    /// <summary>Currency of the product prices.</summary>
    [MaxLength(3)]
    public string MoedaProdutos { get; set; } = Moedas.Brl;

    /// <summary>Reais per 1 unit of <see cref="MoedaProdutos"/>. Always 1 when products are in BRL.</summary>
    public decimal CambioProdutos { get; set; } = 1m;

    /// <summary>Currency of the taxes.</summary>
    [MaxLength(3)]
    public string MoedaImpostos { get; set; } = Moedas.Brl;

    /// <summary>Reais per 1 unit of <see cref="MoedaImpostos"/>. Always 1 when taxes are in BRL.</summary>
    public decimal CambioImpostos { get; set; } = 1m;

    /// <summary>Currency of the shipment. Product currency, tax currency, or BRL.</summary>
    [MaxLength(3)]
    public string MoedaFrete { get; set; } = Moedas.Brl;

    /// <summary>Currency in which the landed cost is stored. Always BRL.</summary>
    [MaxLength(3)]
    public string MoedaDestino { get; set; } = Moedas.Destino;

    /// <summary>Shipment in <see cref="MoedaFrete"/>.</summary>
    public decimal Frete { get; set; }

    /// <summary>Taxes in <see cref="MoedaImpostos"/>.</summary>
    public decimal Impostos { get; set; }

    public ICollection<EntradaEstoqueItem> Itens { get; set; } = new List<EntradaEstoqueItem>();

    public decimal TotalProdutos => Itens.Sum(i => i.Qtd * i.PrecoUnitario);

    [NotMapped]
    public decimal CambioProdutosEfetivo => Moedas.CambioParaDestino(MoedaProdutos, CambioProdutos);

    [NotMapped]
    public decimal CambioImpostosEfetivo => Moedas.CambioParaDestino(MoedaImpostos, CambioImpostos);

    [NotMapped]
    public decimal CambioFreteEfetivo => Moedas.CambioFrete(
        MoedaFrete, MoedaProdutos, CambioProdutos, MoedaImpostos, CambioImpostos);

    [NotMapped]
    public bool MoedasIguais =>
        Moedas.Normalize(MoedaProdutos) == Moedas.Normalize(MoedaImpostos)
        && Moedas.Normalize(MoedaProdutos) == Moedas.Normalize(MoedaFrete);

    public decimal TotalBrl =>
        TotalProdutos * CambioProdutosEfetivo
        + Frete * CambioFreteEfetivo
        + Impostos * CambioImpostosEfetivo;

    /// <summary>Landed line total in BRL, before the unit-cost rounding stored on the item.</summary>
    public decimal SubtotalDestino(EntradaEstoqueItem item)
        => item.Qtd * item.PrecoUnitario * CambioProdutosEfetivo
           + item.FreteRateado * CambioFreteEfetivo
           + item.ImpostoRateado * CambioImpostosEfetivo;
}
