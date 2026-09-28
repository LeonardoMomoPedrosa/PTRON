using PTRON.Models;

namespace PTRON.Services;

/// <summary>
/// Holds the in-progress stock-entry cart for the current Blazor circuit/session.
/// Survives navigation until the entry is finalized or cleared.
/// </summary>
public class EntradaCartState
{
    public List<CartLine> Lines { get; } = new();

    public string Moeda { get; set; } = Moedas.Brl;

    /// <summary>Reais per 1 US$. Used only when <see cref="Moeda"/> is USD.</summary>
    public decimal Cambio { get; set; }

    public decimal Frete { get; set; }

    public decimal Impostos { get; set; }

    public void Clear()
    {
        Lines.Clear();
        Moeda = Moedas.Brl;
        Cambio = 0m;
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
