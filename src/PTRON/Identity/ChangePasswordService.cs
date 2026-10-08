using Microsoft.AspNetCore.Identity;
using PTRON.Email;
using PTRON.Models;

namespace PTRON.Identity;

public sealed record ChangePasswordResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static ChangePasswordResult Ok { get; } = new(true, Array.Empty<string>());

    public static ChangePasswordResult Fail(params string[] errors) => new(false, errors);
}

/// <summary>
/// Changes the password of the signed-in user. Identity rotates the security stamp, which invalidates
/// every other session. The caller must sign the current session in again with the new stamp.
/// </summary>
public sealed class ChangePasswordService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAppEmailSender _email;
    private readonly ILogger<ChangePasswordService> _logger;

    public ChangePasswordService(
        UserManager<ApplicationUser> users,
        IAppEmailSender email,
        ILogger<ChangePasswordService> logger)
    {
        _users = users;
        _email = email;
        _logger = logger;
    }

    public async Task<ChangePasswordResult> ChangeAsync(
        string? userId, string? currentPassword, string? newPassword, string? confirmPassword)
    {
        if (string.IsNullOrEmpty(userId))
            return ChangePasswordResult.Fail("Entre novamente para alterar a senha.");

        if (string.IsNullOrEmpty(currentPassword))
            return ChangePasswordResult.Fail("Informe a senha atual.");

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            return ChangePasswordResult.Fail("As senhas não conferem.");

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
            return ChangePasswordResult.Fail("Entre novamente para alterar a senha.");

        if (!user.EmailConfirmed)
            return ChangePasswordResult.Fail("A conta ainda não está ativa.");

        var result = await _users.ChangePasswordAsync(user, currentPassword, newPassword ?? "");
        if (!result.Succeeded)
            return new ChangePasswordResult(false, result.Errors.Select(e => e.Description).Distinct().ToList());

        if (!string.IsNullOrEmpty(user.Email))
        {
            try
            {
                await _email.SendPasswordChangedAsync(user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar o aviso de senha alterada para o usuário {UserId}.", user.Id);
            }
        }

        return ChangePasswordResult.Ok;
    }
}
