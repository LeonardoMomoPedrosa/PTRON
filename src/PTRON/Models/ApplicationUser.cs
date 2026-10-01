using Microsoft.AspNetCore.Identity;

namespace PTRON.Models;

public class ApplicationUser : IdentityUser
{
    /// <summary>ISO 3166-1 alpha-2 country code (null only for the reserved user).</summary>
    public string? Pais { get; set; }

    /// <summary>Free-text state/province.</summary>
    public string? Estado { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Reserved (emergency) account, e.g. Leo. Never created through sign-up.</summary>
    public bool Reservado { get; set; }
}
