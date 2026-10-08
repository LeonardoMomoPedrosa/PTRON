using PTRON.Models;

namespace PTRON.Services;

/// <summary>
/// Holds the in-progress stock-entry cart for the current Blazor circuit/session.
/// Survives navigation until the entry is finalized or cleared.
/// </summary>
public class EntradaCartState
{
    public List<CartLine> Lines { get; } = new();

    public string MoedaProdutos { get; set; } = Moedas.Brl;

    /// <summary>Reais per 1 unit of <see cref="MoedaProdutos"/>.</summary>
    public decimal CambioProdutos { get; set; }

    public string MoedaImpostos { get; set; } = Moedas.Brl;

    /// <summary>Reais per 1 unit of <see cref="MoedaImpostos"/>, when that currency differs from the products.</summary>
    public decimal CambioImpostos { get; set; }

    /// <summary>Product currency, tax currency, or BRL.</summary>
    public string MoedaFrete { get; set; } = Moedas.Brl;

    public decimal Frete { get; set; }

    public decimal Impostos { get; set; }

    public void Clear()
    {
        Lines.Clear();
        MoedaProdutos = Moedas.Brl;
        CambioProdutos = 0m;
        MoedaImpostos = Moedas.Brl;
        CambioImpostos = 0m;
        MoedaFrete = Moedas.Brl;
        Frete = 0m;
        Impostos = 0m;
    }

    public sealed class CartLine
    {
        public int InsumoId { get; set; }
        public Insumo? Insumo { get; set; }
        public decimal Qtd { get; set; }
        public decimal PrecoUnitario { get; set; }
    }
}
