namespace PTRON.Models;

public class EntradaEstoque
{
    public int Id { get; set; }

    public DateTime Data { get; set; } = DateTime.Now;

    public ICollection<EntradaEstoqueItem> Itens { get; set; } = new List<EntradaEstoqueItem>();
}
