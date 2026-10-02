using System.ComponentModel.DataAnnotations;

namespace PTRON.Models;

public class Equipamento : IUserOwned
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(260)]
    public string? FotoPath { get; set; }

    public ICollection<EquipamentoInsumo> Insumos { get; set; } = new List<EquipamentoInsumo>();
}
