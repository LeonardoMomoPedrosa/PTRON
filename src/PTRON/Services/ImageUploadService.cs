using Microsoft.AspNetCore.Components.Forms;

namespace PTRON.Services;

public class ImageUploadService
{
    // Camera photos are often well above 5 MB.
    private const long MaxFileSize = 20 * 1024 * 1024;
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
        return await SaveAsync(stream, file.Name, file.Size, file.ContentType, cancellationToken);
    }

    /// <summary>
    /// Saves an HTTP multipart upload (Android / REST clients).
    /// </summary>
    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        return await SaveAsync(stream, file.FileName, file.Length, file.ContentType, cancellationToken);
    }

    public Task<string> SaveAsync(
        Stream stream, string fileName, long length, CancellationToken cancellationToken = default)
        => SaveAsync(stream, fileName, length, contentType: null, cancellationToken);

    public async Task<string> SaveAsync(
        Stream stream, string fileName, long length, string? contentType, CancellationToken cancellationToken = default)
    {
        var extension = ResolveExtension(fileName, contentType);

        if (length > MaxFileSize)
        {
            throw new InvalidOperationException("A imagem excede o tamanho máximo de 20 MB.");
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
    /// Phone pickers sometimes hand back a name with no extension (or a generic one).
    /// Fall back to the MIME type so a JPEG from the camera is still stored.
    /// </summary>
    private static string ResolveExtension(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (AllowedExtensions.Contains(extension))
        {
            return extension;
        }

        var fromType = contentType?.Split(';', 2)[0].Trim().ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => null
        };

        if (fromType is not null)
        {
            return fromType;
        }

        throw new InvalidOperationException(
            string.IsNullOrEmpty(extension)
                ? "Formato de imagem não suportado. Use JPG, PNG, GIF ou WEBP."
                : $"Formato de imagem não suportado ({extension}). Use JPG, PNG, GIF ou WEBP.");
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
