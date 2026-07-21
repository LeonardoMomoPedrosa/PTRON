namespace PTRON.Models;

public class ProdutoInsumo
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    public decimal PrecoUnitario { get; set; }

    public decimal Qtd { get; set; }
}
