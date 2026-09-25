using Microsoft.AspNetCore.Mvc;
using PTRON.Services;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api/produtos")]
public sealed class ProdutosController : ControllerBase
{
    private readonly ProdutoService _service;

    public ProdutosController(ProdutoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProdutoListDto>>> GetAll()
    {
        var produtos = await _service.GetAllAsync();
        return Ok(produtos.Select(DtoMapper.ToListDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoDto>> GetById(int id)
    {
        var produto = await _service.GetWithInsumosAsync(id);
        if (produto is null)
        {
            return NotFound(new ErrorDto { Error = "Produto não encontrado." });
        }

        return Ok(DtoMapper.ToDto(produto));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
