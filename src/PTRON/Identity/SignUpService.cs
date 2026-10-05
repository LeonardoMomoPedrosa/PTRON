using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PTRON.Email;
using PTRON.Models;

namespace PTRON.Identity;

public sealed class SignUpRequest
{
    public string? Email { get; set; }
    public string? Pais { get; set; }
    public string? Estado { get; set; }
    public string? Senha { get; set; }
}

/// <summary>
/// Outcome of a sign-up attempt. <see cref="Succeeded"/> is also true when the e-mail already
/// belongs to an account, so the caller cannot tell new from existing e-mails (no account enumeration).
/// </summary>
public sealed record SignUpResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static SignUpResult Ok { get; } = new(true, Array.Empty<string>());

    public static SignUpResult Fail(params string[] errors) => new(false, errors);
}

public sealed class SignUpService
{
    public const int MaxEmailLength = 256;
    public const int MaxEstadoLength = 100;

    private readonly UserManager<ApplicationUser> _users;
    private readonly IAppEmailSender _email;
    private readonly LeoOptions _leo;
    private readonly ILogger<SignUpService> _logger;

    public SignUpService(
        UserManager<ApplicationUser> users,
        IAppEmailSender email,
        IOptions<LeoOptions> leo,
        ILogger<SignUpService> logger)
    {
        _users = users;
        _email = email;
        _leo = leo.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates an inactive account and sends the activation e-mail. The account only
    /// signs in after the e-mail is confirmed (E8-S5).
    /// </summary>
    public async Task<SignUpResult> RegisterAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim() ?? "";
        var estado = request.Estado?.Trim() ?? "";
        var pais = request.Pais?.Trim().ToUpperInvariant() ?? "";
        var senha = request.Senha ?? "";

        var errors = new List<string>();
        if (!IsValidEmail(email))
            errors.Add("Informe um e-mail válido.");
        if (!Paises.IsValid(pais))
            errors.Add("Selecione o país.");
        if (estado.Length == 0)
            errors.Add("Informe o estado.");
        else if (estado.Length > MaxEstadoLength)
            errors.Add($"O estado deve ter no máximo {MaxEstadoLength} caracteres.");
        if (senha.Length == 0)
            errors.Add("Informe a senha.");
        if (errors.Count > 0)
            return new SignUpResult(false, errors);

        if (ReservedUsers.IsReservedEmail(email, _leo.Email))
            return SignUpResult.Fail("Este e-mail não está disponível.");

        if (await _users.FindByEmailAsync(email) is not null)
            return SignUpResult.Ok;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            Pais = pais,
            Estado = estado,
            CriadoEm = DateTime.UtcNow,
        };

        var created = await _users.CreateAsync(user, senha);
        if (!created.Succeeded)
        {
            // Lost a race with another sign-up for the same e-mail: answer like any existing account.
            if (created.Errors.All(e => e.Code is nameof(IdentityErrorDescriber.DuplicateEmail)
                                         or nameof(IdentityErrorDescriber.DuplicateUserName)))
                return SignUpResult.Ok;

            return new SignUpResult(false, created.Errors.Select(e => e.Description).Distinct().ToList());
        }

        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        try
        {
            await _email.SendActivationAsync(email, user.Id, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar o e-mail de ativação para o novo usuário {UserId}.", user.Id);
        }

        return SignUpResult.Ok;
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Length == 0 || email.Length > MaxEmailLength)
            return false;

        return MailAddress.TryCreate(email, out var parsed)
               && string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase)
               && parsed.Host.Contains('.');
    }
}
