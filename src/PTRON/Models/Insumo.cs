using System.ComponentModel.DataAnnotations;

namespace PTRON.Models;

public class Insumo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O tipo é obrigatório.")]
    public int TipoInsumoId { get; set; }
    public TipoInsumo? TipoInsumo { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Valor { get; set; }

    [MaxLength(50)]
    public string? Potencia { get; set; }

    [MaxLength(50)]
    public string? Voltagem { get; set; }

    public decimal Saldo { get; set; }

    public decimal CustoUnitario { get; set; }

    [MaxLength(260)]
    public string? FotoPath { get; set; }

    public ICollection<EquipamentoInsumo> EquipamentoInsumos { get; set; } = new List<EquipamentoInsumo>();
}
