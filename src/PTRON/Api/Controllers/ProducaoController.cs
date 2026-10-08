using Microsoft.AspNetCore.Mvc;
using PTRON.Services;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api/producao")]
public sealed class ProducaoController : ControllerBase
{
    private readonly ProducaoService _producao;
    private readonly ProdutoService _produtos;

    public ProducaoController(ProducaoService producao, ProdutoService produtos)
    {
        _producao = producao;
        _produtos = produtos;
    }

    [HttpGet("preview/{equipamentoId:int}")]
    public async Task<ActionResult<ProducaoPreviewDto>> Preview(int equipamentoId, [FromQuery] int quantidade = 1)
    {
        var preview = await _producao.GetPreviewAsync(equipamentoId, quantidade);
        if (preview is null)
        {
            return NotFound(new ErrorDto { Error = "Equipamento não encontrado." });
        }

        return Ok(DtoMapper.ToDto(preview));
    }

    [HttpPost]
    public async Task<ActionResult<ProducaoResultadoDto>> Produzir(ProduzirDto dto)
    {
        var resultado = await _producao.ProduzirAsync(dto.EquipamentoId, dto.DescricaoAdicional, dto.Quantidade);
        var produtos = new List<ProdutoDto>();
        foreach (var produto in resultado.Produtos)
        {
            var created = await _produtos.GetWithInsumosAsync(produto.Id) ?? produto;
            produtos.Add(DtoMapper.ToDto(created));
        }

        var primeiro = resultado.Produtos[0];
        return Created($"/api/produtos/{primeiro.Id}", new ProducaoResultadoDto
        {
            Quantidade = resultado.Quantidade,
            CustoUnitario = resultado.CustoUnitario,
            CustoTotal = resultado.CustoUnitario * resultado.Quantidade,
            Produtos = produtos
        });
    }
}
