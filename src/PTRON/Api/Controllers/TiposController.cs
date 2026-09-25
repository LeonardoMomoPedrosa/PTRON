using Microsoft.AspNetCore.Mvc;
using PTRON.Models;
using PTRON.Services;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api/tipos")]
public sealed class TiposController : ControllerBase
{
    private readonly TipoInsumoService _service;

    public TiposController(TipoInsumoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<TipoDto>>> GetAll()
    {
        var tipos = await _service.GetAllAsync();
        var counts = await _service.CountInsumosByTipoAsync();
        return Ok(tipos.Select(t => DtoMapper.ToDto(t, counts.GetValueOrDefault(t.Id))).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoDto>> GetById(int id)
    {
        var tipo = await _service.GetAsync(id);
        if (tipo is null)
        {
            return NotFound(new ErrorDto { Error = "Tipo não encontrado." });
        }

        return Ok(DtoMapper.ToDto(tipo, await _service.CountInsumosAsync(tipo.Id)));
    }

    [HttpPost]
    public async Task<ActionResult<TipoDto>> Create(TipoWriteDto dto)
    {
        var tipo = new TipoInsumo { Nome = dto.Nome };
        await _service.CreateAsync(tipo);
        var created = DtoMapper.ToDto(tipo, 0);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TipoDto>> Update(int id, TipoWriteDto dto)
    {
        await _service.UpdateAsync(new TipoInsumo { Id = id, Nome = dto.Nome });
        var updated = await _service.GetAsync(id);
        return Ok(DtoMapper.ToDto(updated!, await _service.CountInsumosAsync(id)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
