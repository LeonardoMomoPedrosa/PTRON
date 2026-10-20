using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PTRON.Models;
using PTRON.Localization;

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

        if (ReservedUsers.IsReservedName(user.UserName) || ReservedUsers.IsReservedEmail(user.Email, _leo.Email))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "ReservedUser",
                Description = StaticText.Get("Este e-mail não está disponível.")
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
