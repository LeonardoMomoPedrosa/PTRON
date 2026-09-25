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
    public async Task<ActionResult<ProducaoPreviewDto>> Preview(int equipamentoId)
    {
        var preview = await _producao.GetPreviewAsync(equipamentoId);
        if (preview is null)
        {
            return NotFound(new ErrorDto { Error = "Equipamento não encontrado." });
        }

        return Ok(DtoMapper.ToDto(preview));
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoDto>> Produzir(ProduzirDto dto)
    {
        var produto = await _producao.ProduzirAsync(dto.EquipamentoId, dto.DescricaoAdicional);
        var created = await _produtos.GetWithInsumosAsync(produto.Id) ?? produto;
        return Created($"/api/produtos/{produto.Id}", DtoMapper.ToDto(created));
    }
}
