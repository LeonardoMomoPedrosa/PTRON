using System.ComponentModel.DataAnnotations;

namespace PTRON.Models;

public class TipoInsumo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}
