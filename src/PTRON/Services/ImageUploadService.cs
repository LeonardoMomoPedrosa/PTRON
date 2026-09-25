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
        await using var stream = file.OpenReadStream(MaxFileSize, cancellationToken);
        return await SaveAsync(stream, file.Name, file.Size, cancellationToken);
    }

    /// <summary>
    /// Saves an HTTP multipart upload (Android / REST clients).
    /// </summary>
    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        return await SaveAsync(stream, file.FileName, file.Length, cancellationToken);
    }

    public async Task<string> SaveAsync(
        Stream stream, string fileName, long length, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Formato de imagem não suportado ({extension}). Use JPG, PNG, GIF ou WEBP.");
        }

        if (length > MaxFileSize)
        {
            throw new InvalidOperationException("A imagem excede o tamanho máximo de 5 MB.");
        }

        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, storedName);

        await using (var output = File.Create(fullPath))
        {
            await stream.CopyToAsync(output, cancellationToken);
        }

        return $"/uploads/{storedName}";
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
