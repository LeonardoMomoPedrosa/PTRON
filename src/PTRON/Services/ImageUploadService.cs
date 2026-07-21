using Microsoft.AspNetCore.Components.Forms;

namespace PTRON.Services;

public class ImageUploadService
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly IWebHostEnvironment _env;

    public ImageUploadService(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Saves an uploaded image to wwwroot/uploads and returns the relative web path (e.g. "/uploads/xyz.png").
    /// Throws InvalidOperationException for invalid files.
    /// </summary>
    public async Task<string> SaveAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Formato de imagem não suportado ({extension}). Use JPG, PNG, GIF ou WEBP.");
        }

        if (file.Size > MaxFileSize)
        {
            throw new InvalidOperationException("A imagem excede o tamanho máximo de 5 MB.");
        }

        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = File.Create(fullPath))
        {
            await file.OpenReadStream(MaxFileSize, cancellationToken).CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/{fileName}";
    }

    /// <summary>
    /// Deletes a previously stored image given its relative web path. Safe to call with null/empty.
    /// </summary>
    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/', '\\'));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
