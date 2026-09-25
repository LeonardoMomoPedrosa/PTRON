using Microsoft.AspNetCore.Mvc;
using PTRON.Services;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public sealed class UploadsController : ControllerBase
{
    private readonly ImageUploadService _images;

    public UploadsController(ImageUploadService images)
    {
        _images = images;
    }

    /// <summary>
    /// Upload a photo (JPG, PNG, GIF or WEBP, max 5 MB).
    /// Multipart field name: file.
    /// Returns a relative path to store in insumo/equipamento fotoPath.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadDto>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ErrorDto { Error = "Envie um arquivo de imagem no campo \"file\"." });
        }

        var path = await _images.SaveAsync(file, cancellationToken);
        return Ok(new UploadDto { FotoPath = path });
    }
}
