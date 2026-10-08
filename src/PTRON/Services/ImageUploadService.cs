using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;
using PTRON.Identity;

namespace PTRON.Services;

public class ImageUploadService
{
    // Camera photos are often well above 5 MB.
    private const long MaxFileSize = 20 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly Regex UserIdPattern = new("^[A-Za-z0-9-]{1,64}$", RegexOptions.CultureInvariant);

    private readonly IWebHostEnvironment _env;
    private readonly ICurrentUser _currentUser;

    public ImageUploadService(IWebHostEnvironment env, ICurrentUser currentUser)
    {
        _env = env;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Saves an uploaded image under wwwroot/uploads/{userId}/ and returns the web path
    /// (e.g. "/uploads/leo/xyz.png"). Throws InvalidOperationException for invalid files.
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

        var userId = RequireUserId();
        var userDir = Path.Combine(_env.WebRootPath, "uploads", userId);
        Directory.CreateDirectory(userDir);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(userDir, storedName);

        await using (var output = File.Create(fullPath))
        {
            await stream.CopyToAsync(output, cancellationToken);
        }

        return $"/uploads/{userId}/{storedName}";
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
    /// Deletes a previously stored image when it belongs to the current user.
    /// Photos saved before per-user folders (<c>/uploads/file.png</c>) belong to Leo.
    /// Paths of other users, or paths that leave the uploads folder, are ignored.
    /// </summary>
    public void Delete(string? relativePath)
    {
        var fullPath = TryResolveOwnedFile(relativePath);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string RequireUserId()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId) || !UserIdPattern.IsMatch(userId))
        {
            throw new InvalidOperationException("Não há usuário autenticado para salvar a imagem.");
        }

        return userId;
    }

    private string? TryResolveOwnedFile(string? relativePath)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId) || !UserIdPattern.IsMatch(userId))
            return null;

        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var path = relativePath.Replace('\\', '/').Trim();
        if (path.Contains("..", StringComparison.Ordinal))
            return null;

        const string prefix = "/uploads/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = path[prefix.Length..].Split('/', StringSplitOptions.RemoveEmptyEntries);
        string relativeToWebRoot;
        if (parts.Length == 2
            && string.Equals(parts[0], userId, StringComparison.Ordinal)
            && IsSafeFileName(parts[1]))
        {
            relativeToWebRoot = Path.Combine("uploads", userId, parts[1]);
        }
        else if (parts.Length == 1
                 && userId == ReservedUsers.LeoId
                 && IsSafeFileName(parts[0]))
        {
            relativeToWebRoot = Path.Combine("uploads", parts[0]);
        }
        else
        {
            return null;
        }

        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));
        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relativeToWebRoot));
        var rootWithSeparator = uploadsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            return null;

        return full;
    }

    private static bool IsSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128 || name is "." or "..")
            return false;

        return name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
               && name == Path.GetFileName(name);
    }
}
