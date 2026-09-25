using Microsoft.AspNetCore.Mvc;
using PTRON.Models;
using PTRON.Services;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api/entradas")]
public sealed class EntradasEstoqueController : ControllerBase
{
    private readonly EntradaEstoqueService _service;

    public EntradasEstoqueController(EntradaEstoqueService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<EntradaEstoqueDto>>> GetHistorico()
    {
        var historico = await _service.GetHistoricoAsync();
        return Ok(historico.Select(DtoMapper.ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EntradaEstoqueDto>> GetById(int id)
    {
        var entrada = await _service.GetAsync(id);
        if (entrada is null)
        {
            return NotFound(new ErrorDto { Error = "Entrada de estoque não encontrada." });
        }

        return Ok(DtoMapper.ToDto(entrada));
    }

    [HttpPost]
    public async Task<ActionResult<EntradaEstoqueDto>> Finalizar(EntradaEstoqueWriteDto dto)
    {
        var itens = dto.Itens.Select(i => new EntradaEstoqueItem
        {
            InsumoId = i.InsumoId,
            Qtd = i.Qtd,
            PrecoUnitario = i.PrecoUnitario
        }).ToList();

        var entrada = await _service.FinalizarAsync(itens);
        var created = await _service.GetAsync(entrada.Id) ?? entrada;
        return CreatedAtAction(nameof(GetById), new { id = entrada.Id }, DtoMapper.ToDto(created));
    }
}
