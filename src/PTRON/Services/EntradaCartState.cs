using PTRON.Models;

namespace PTRON.Services;

/// <summary>
/// Holds the in-progress stock-entry cart for the current Blazor circuit/session.
/// Survives navigation until the entry is finalized or cleared.
/// </summary>
public class EntradaCartState
{
    public List<CartLine> Lines { get; } = new();

    public void Clear() => Lines.Clear();

    public sealed class CartLine
    {
        public int InsumoId { get; set; }
        public Insumo? Insumo { get; set; }
        public decimal Qtd { get; set; }
        public decimal PrecoUnitario { get; set; }
    }
}
