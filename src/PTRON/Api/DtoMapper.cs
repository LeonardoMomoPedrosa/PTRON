using PTRON.Models;
using PTRON.Services;

namespace PTRON.Api;

internal static class DtoMapper
{
    public static TipoDto ToDto(TipoInsumo tipo, int insumosCount) => new()
    {
        Id = tipo.Id,
        Nome = tipo.Nome,
        InsumosCount = insumosCount
    };

    public static InsumoDto ToDto(Insumo insumo) => new()
    {
        Id = insumo.Id,
        TipoInsumoId = insumo.TipoInsumoId,
        TipoNome = insumo.TipoInsumo?.Nome ?? string.Empty,
        Nome = insumo.Nome,
        Valor = insumo.Valor,
        Potencia = insumo.Potencia,
        Voltagem = insumo.Voltagem,
        Saldo = insumo.Saldo,
        CustoUnitario = insumo.CustoUnitario,
        FotoPath = insumo.FotoPath
    };

    public static Insumo ToModel(InsumoWriteDto dto, int id = 0) => new()
    {
        Id = id,
        TipoInsumoId = dto.TipoInsumoId,
        Nome = dto.Nome,
        Valor = dto.Valor,
        Potencia = dto.Potencia,
        Voltagem = dto.Voltagem,
        FotoPath = dto.FotoPath
    };

    public static EquipamentoListDto ToListDto(Equipamento equipamento) => new()
    {
        Id = equipamento.Id,
        Nome = equipamento.Nome,
        FotoPath = equipamento.FotoPath,
        InsumosCount = equipamento.Insumos.Count
    };

    public static EquipamentoDto ToDto(Equipamento equipamento) => new()
    {
        Id = equipamento.Id,
        Nome = equipamento.Nome,
        FotoPath = equipamento.FotoPath,
        Insumos = equipamento.Insumos
            .Select(ei => new BomItemDto
            {
                InsumoId = ei.InsumoId,
                InsumoNome = ei.Insumo?.Nome ?? string.Empty,
                TipoNome = ei.Insumo?.TipoInsumo?.Nome,
                Qtd = ei.Qtd
            })
            .ToList()
    };

    public static Equipamento ToModel(EquipamentoWriteDto dto, int id = 0) => new()
    {
        Id = id,
        Nome = dto.Nome,
        FotoPath = dto.FotoPath,
        Insumos = (dto.Insumos ?? new List<BomItemWriteDto>())
            .Select(item => new EquipamentoInsumo
            {
                InsumoId = item.InsumoId,
                Qtd = item.Qtd
            })
            .ToList()
    };

    public static EntradaEstoqueDto ToDto(EntradaEstoque entrada) => new()
    {
        Id = entrada.Id,
        Data = entrada.Data,
        Total = entrada.Itens.Sum(i => i.Qtd * i.PrecoUnitario),
        Itens = entrada.Itens
            .Select(i => new EntradaEstoqueItemDto
            {
                InsumoId = i.InsumoId,
                InsumoNome = i.Insumo?.Nome ?? string.Empty,
                TipoNome = i.Insumo?.TipoInsumo?.Nome,
                Qtd = i.Qtd,
                PrecoUnitario = i.PrecoUnitario
            })
            .ToList()
    };

    public static ProducaoPreviewDto ToDto(ProducaoPreview preview) => new()
    {
        EquipamentoId = preview.EquipamentoId,
        EquipamentoNome = preview.EquipamentoNome,
        CustoEstimado = preview.CustoEstimado,
        PodeProduzir = preview.PodeProduzir,
        Linhas = preview.Linhas.Select(ToDto).ToList(),
        Faltantes = preview.Faltantes.Select(ToDto).ToList()
    };

    public static BomLinhaPreviewDto ToDto(BomLinhaPreview linha) => new()
    {
        InsumoId = linha.InsumoId,
        InsumoNome = linha.InsumoNome,
        QtdNecessaria = linha.QtdNecessaria,
        SaldoDisponivel = linha.SaldoDisponivel,
        CustoUnitario = linha.CustoUnitario,
        Subtotal = linha.Subtotal,
        Faltante = linha.Faltante,
        Disponivel = linha.Disponivel
    };

    public static ProdutoListDto ToListDto(Produto produto) => new()
    {
        Id = produto.Id,
        EquipamentoId = produto.EquipamentoId,
        EquipamentoNome = produto.Equipamento?.Nome ?? string.Empty,
        DescricaoAdicional = produto.DescricaoAdicional,
        Data = produto.Data,
        CustoTotal = produto.CustoTotal
    };

    public static ProdutoDto ToDto(Produto produto) => new()
    {
        Id = produto.Id,
        EquipamentoId = produto.EquipamentoId,
        EquipamentoNome = produto.Equipamento?.Nome ?? string.Empty,
        DescricaoAdicional = produto.DescricaoAdicional,
        Data = produto.Data,
        CustoTotal = produto.CustoTotal,
        Insumos = produto.Insumos
            .Select(pi => new ProdutoInsumoDto
            {
                InsumoId = pi.InsumoId,
                InsumoNome = pi.Insumo?.Nome ?? string.Empty,
                Qtd = pi.Qtd,
                PrecoUnitario = pi.PrecoUnitario
            })
            .ToList()
    };
}
