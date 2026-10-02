using System.ComponentModel.DataAnnotations;

namespace PTRON.Models;

public class TipoInsumo : IUserOwned
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}
