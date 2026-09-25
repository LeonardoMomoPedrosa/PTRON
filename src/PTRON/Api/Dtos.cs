using System.ComponentModel.DataAnnotations;

namespace PTRON.Api;

public sealed class ErrorDto
{
    public string Error { get; set; } = string.Empty;
}

public sealed class ApiInfoDto
{
    public string Name { get; set; } = "PTRON";
    public string Version { get; set; } = "1.0";
}

public sealed class TipoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int InsumosCount { get; set; }
}

public sealed class TipoWriteDto
{
    [Required(ErrorMessage = "Informe o nome.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
}

public sealed class InsumoDto
{
    public int Id { get; set; }
    public int TipoInsumoId { get; set; }
    public string TipoNome { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Valor { get; set; }
    public string? Potencia { get; set; }
    public string? Voltagem { get; set; }
    public decimal Saldo { get; set; }
    public decimal CustoUnitario { get; set; }
    public string? FotoPath { get; set; }
}

public sealed class InsumoWriteDto
{
    [Required(ErrorMessage = "Selecione o tipo do insumo.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione o tipo do insumo.")]
    public int TipoInsumoId { get; set; }

    [Required(ErrorMessage = "Informe o nome.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Valor { get; set; }

    [MaxLength(50)]
    public string? Potencia { get; set; }

    [MaxLength(50)]
    public string? Voltagem { get; set; }

    /// <summary>Relative path returned by POST /api/uploads, e.g. /uploads/abc.png.</summary>
    [MaxLength(260)]
    public string? FotoPath { get; set; }
}

public sealed class EquipamentoListDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? FotoPath { get; set; }
    public int InsumosCount { get; set; }
}

public sealed class EquipamentoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? FotoPath { get; set; }
    public List<BomItemDto> Insumos { get; set; } = new();
}

public sealed class BomItemDto
{
    public int InsumoId { get; set; }
    public string InsumoNome { get; set; } = string.Empty;
    public string? TipoNome { get; set; }
    public decimal Qtd { get; set; }
}

public sealed class EquipamentoWriteDto
{
    [Required(ErrorMessage = "Informe o nome.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(260)]
    public string? FotoPath { get; set; }

    public List<BomItemWriteDto> Insumos { get; set; } = new();
}

public sealed class BomItemWriteDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione o insumo.")]
    public int InsumoId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335", ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Qtd { get; set; }
}

public sealed class EntradaEstoqueDto
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public decimal Total { get; set; }
    public List<EntradaEstoqueItemDto> Itens { get; set; } = new();
}

public sealed class EntradaEstoqueItemDto
{
    public int InsumoId { get; set; }
    public string InsumoNome { get; set; } = string.Empty;
    public string? TipoNome { get; set; }
    public decimal Qtd { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => Qtd * PrecoUnitario;
}

public sealed class EntradaEstoqueWriteDto
{
    [MinLength(1, ErrorMessage = "Adicione ao menos um item à entrada.")]
    public List<EntradaEstoqueItemWriteDto> Itens { get; set; } = new();
}

public sealed class EntradaEstoqueItemWriteDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione o insumo.")]
    public int InsumoId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335", ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Qtd { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "O preço não pode ser negativo.")]
    public decimal PrecoUnitario { get; set; }
}

public sealed class ProducaoPreviewDto
{
    public int EquipamentoId { get; set; }
    public string EquipamentoNome { get; set; } = string.Empty;
    public List<BomLinhaPreviewDto> Linhas { get; set; } = new();
    public decimal CustoEstimado { get; set; }
    public bool PodeProduzir { get; set; }
    public List<BomLinhaPreviewDto> Faltantes { get; set; } = new();
}

public sealed class BomLinhaPreviewDto
{
    public int InsumoId { get; set; }
    public string InsumoNome { get; set; } = string.Empty;
    public decimal QtdNecessaria { get; set; }
    public decimal SaldoDisponivel { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Faltante { get; set; }
    public bool Disponivel { get; set; }
}

public sealed class ProduzirDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione o equipamento.")]
    public int EquipamentoId { get; set; }

    [MaxLength(1000)]
    public string? DescricaoAdicional { get; set; }
}

public sealed class ProdutoListDto
{
    public int Id { get; set; }
    public int EquipamentoId { get; set; }
    public string EquipamentoNome { get; set; } = string.Empty;
    public string? DescricaoAdicional { get; set; }
    public DateTime Data { get; set; }
    public decimal CustoTotal { get; set; }
}

public sealed class ProdutoDto
{
    public int Id { get; set; }
    public int EquipamentoId { get; set; }
    public string EquipamentoNome { get; set; } = string.Empty;
    public string? DescricaoAdicional { get; set; }
    public DateTime Data { get; set; }
    public decimal CustoTotal { get; set; }
    public List<ProdutoInsumoDto> Insumos { get; set; } = new();
}

public sealed class ProdutoInsumoDto
{
    public int InsumoId { get; set; }
    public string InsumoNome { get; set; } = string.Empty;
    public decimal Qtd { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => Qtd * PrecoUnitario;
}

public sealed class UploadDto
{
    public string FotoPath { get; set; } = string.Empty;
}
