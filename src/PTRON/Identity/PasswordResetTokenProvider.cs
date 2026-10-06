using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PTRON.Models;

namespace PTRON.Identity;

public sealed class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
{
}

/// <summary>Password reset links live for 1 hour while activation links keep the 24 hour default.</summary>
public sealed class PasswordResetTokenProvider : DataProtectorTokenProvider<ApplicationUser>
{
    public const string ProviderName = "PtronPasswordReset";

    public PasswordResetTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<PasswordResetTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}
