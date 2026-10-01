using Microsoft.AspNetCore.Identity;
using PTRON.Models;

namespace PTRON.Identity;

/// <summary>Identity has no "requires a letter" option; digits are enforced by <see cref="PasswordOptions"/>.</summary>
public sealed class LetterPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (!string.IsNullOrEmpty(password) && !password.Any(char.IsLetter))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordRequiresLetter",
                Description = "A senha deve ter pelo menos uma letra."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
