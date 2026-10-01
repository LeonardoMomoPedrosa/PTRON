using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PTRON.Models;

namespace PTRON.Identity;

/// <summary>Prevents regular accounts from taking the reserved name / email of the Leo account.</summary>
public sealed class ReservedUserValidator : IUserValidator<ApplicationUser>
{
    private readonly LeoOptions _leo;

    public ReservedUserValidator(IOptions<LeoOptions> leo)
    {
        _leo = leo.Value;
    }

    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        if (user.Reservado || user.Id == ReservedUsers.LeoId)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        var reservedName = ReservedUsers.IsReservedName(user.UserName) || ReservedUsers.IsReservedName(user.Email);
        var reservedEmail = !string.IsNullOrWhiteSpace(_leo.Email)
            && string.Equals(user.Email?.Trim(), _leo.Email.Trim(), StringComparison.OrdinalIgnoreCase);

        if (reservedName || reservedEmail)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "ReservedUser",
                Description = "Este e-mail não está disponível."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
