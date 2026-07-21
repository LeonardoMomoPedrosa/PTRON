using System.ComponentModel.DataAnnotations;

namespace PTRON.Models;

public class Produto
{
    public int Id { get; set; }

    public int EquipamentoId { get; set; }
    public Equipamento? Equipamento { get; set; }

    [MaxLength(1000)]
    public string? DescricaoAdicional { get; set; }

    public DateTime Data { get; set; } = DateTime.Now;

    public decimal CustoTotal { get; set; }

    public ICollection<ProdutoInsumo> Insumos { get; set; } = new List<ProdutoInsumo>();
}
