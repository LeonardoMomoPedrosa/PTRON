using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Services;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api/entradas")]
public sealed class EntradasEstoqueController : ControllerBase
{
    private readonly EntradaEstoqueService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EntradasEstoqueController(EntradaEstoqueService service, IDbContextFactory<AppDbContext> dbFactory)
    {
        _service = service;
        _dbFactory = dbFactory;
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

    [HttpPost("preview")]
    public async Task<ActionResult<EntradaEstoqueDto>> Preview(EntradaEstoqueWriteDto dto)
    {
        var rateio = _service.Preview(ToCommand(dto));
        var insumos = await LoadInsumosAsync(rateio.Linhas.Select(l => l.InsumoId));
        return Ok(DtoMapper.ToDto(rateio, insumos));
    }

    [HttpPost]
    public async Task<ActionResult<EntradaEstoqueDto>> Finalizar(EntradaEstoqueWriteDto dto)
    {
        var entrada = await _service.FinalizarAsync(ToCommand(dto));
        var created = await _service.GetAsync(entrada.Id) ?? entrada;
        return CreatedAtAction(nameof(GetById), new { id = entrada.Id }, DtoMapper.ToDto(created));
    }

    private static EntradaCommand ToCommand(EntradaEstoqueWriteDto dto) => new()
    {
        Moeda = dto.Moeda,
        Cambio = dto.Cambio,
        Frete = dto.Frete,
        Impostos = dto.Impostos,
        Itens = dto.Itens.Select(i => new EntradaLinhaInput
        {
            InsumoId = i.InsumoId,
            Qtd = i.Qtd,
            PrecoUnitario = i.PrecoUnitario
        }).ToList()
    };

    private async Task<IReadOnlyDictionary<int, Models.Insumo>> LoadInsumosAsync(IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var insumos = await db.Insumos
            .Include(i => i.TipoInsumo)
            .AsNoTracking()
            .Where(i => idList.Contains(i.Id))
            .ToListAsync();
        return insumos.ToDictionary(i => i.Id);
    }
}
