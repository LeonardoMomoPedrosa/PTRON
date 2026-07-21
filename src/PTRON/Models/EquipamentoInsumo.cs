using System.ComponentModel.DataAnnotations;

namespace PTRON.Models;

public class EquipamentoInsumo
{
    public int Id { get; set; }

    public int EquipamentoId { get; set; }
    public Equipamento? Equipamento { get; set; }

    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Qtd { get; set; }
}
