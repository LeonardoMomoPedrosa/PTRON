using Microsoft.AspNetCore.Mvc;
using PTRON.Services;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api/equipamentos")]
public sealed class EquipamentosController : ControllerBase
{
    private readonly EquipamentoService _service;

    public EquipamentosController(EquipamentoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<EquipamentoListDto>>> GetAll()
    {
        var equipamentos = await _service.GetAllAsync();
        return Ok(equipamentos.Select(DtoMapper.ToListDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipamentoDto>> GetById(int id)
    {
        var equipamento = await _service.GetWithBomAsync(id);
        if (equipamento is null)
        {
            return NotFound(new ErrorDto { Error = "Equipamento não encontrado." });
        }

        return Ok(DtoMapper.ToDto(equipamento));
    }

    [HttpPost]
    public async Task<ActionResult<EquipamentoDto>> Create(EquipamentoWriteDto dto)
    {
        var equipamento = DtoMapper.ToModel(dto);
        await _service.CreateAsync(equipamento);
        var created = await _service.GetWithBomAsync(equipamento.Id);
        return CreatedAtAction(nameof(GetById), new { id = equipamento.Id }, DtoMapper.ToDto(created!));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EquipamentoDto>> Update(int id, EquipamentoWriteDto dto)
    {
        await _service.UpdateAsync(DtoMapper.ToModel(dto, id));
        var updated = await _service.GetWithBomAsync(id);
        return Ok(DtoMapper.ToDto(updated!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
