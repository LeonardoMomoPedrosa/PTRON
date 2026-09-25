using Microsoft.AspNetCore.Mvc;
using PTRON.Services;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api/insumos")]
public sealed class InsumosController : ControllerBase
{
    private readonly InsumoService _service;

    public InsumosController(InsumoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<InsumoDto>>> GetAll(
        [FromQuery] string? search, [FromQuery] int? tipoId)
    {
        var insumos = await _service.GetAllAsync(search, tipoId);
        return Ok(insumos.Select(DtoMapper.ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InsumoDto>> GetById(int id)
    {
        var insumo = await _service.GetAsync(id);
        if (insumo is null)
        {
            return NotFound(new ErrorDto { Error = "Insumo não encontrado." });
        }

        return Ok(DtoMapper.ToDto(insumo));
    }

    [HttpPost]
    public async Task<ActionResult<InsumoDto>> Create(InsumoWriteDto dto)
    {
        var insumo = DtoMapper.ToModel(dto);
        await _service.CreateAsync(insumo);
        var created = await _service.GetAsync(insumo.Id);
        return CreatedAtAction(nameof(GetById), new { id = insumo.Id }, DtoMapper.ToDto(created!));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InsumoDto>> Update(int id, InsumoWriteDto dto)
    {
        await _service.UpdateAsync(DtoMapper.ToModel(dto, id));
        var updated = await _service.GetAsync(id);
        return Ok(DtoMapper.ToDto(updated!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
