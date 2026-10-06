using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Email;
using PTRON.Models;

namespace PTRON.Identity;

public enum ActivationOutcome
{
    Activated,
    AlreadyActive,
    Invalid,
}

/// <summary>
/// Confirms the e-mail behind an activation link, gives the new user the default types and
/// (re)sends activation e-mails, rate limited per address.
/// </summary>
public sealed class ActivationService
{
    public static readonly IReadOnlyList<string> DefaultTipos =
        new[] { "Resistor", "Capacitor", "Diodo", "Transistor", "CI", "Válvula" };

    private readonly UserManager<ApplicationUser> _users;
    private readonly IAppEmailSender _email;
    private readonly ActivationEmailLimiter _limiter;
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly ILogger<ActivationService> _logger;

    public ActivationService(
        UserManager<ApplicationUser> users,
        IAppEmailSender email,
        ActivationEmailLimiter limiter,
        DbContextOptions<AppDbContext> dbOptions,
        ILogger<ActivationService> logger)
    {
        _users = users;
        _email = email;
        _limiter = limiter;
        _dbOptions = dbOptions;
        _logger = logger;
    }

    public async Task<ActivationOutcome> ActivateAsync(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return ActivationOutcome.Invalid;

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
            return ActivationOutcome.Invalid;

        if (user.EmailConfirmed)
            return ActivationOutcome.AlreadyActive;

        var confirmed = await _users.ConfirmEmailAsync(user, token);
        if (!confirmed.Succeeded)
            return ActivationOutcome.Invalid;

        try
        {
            await EnsureDefaultTiposAsync(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao criar os tipos padrão do usuário {UserId}.", user.Id);
        }

        return ActivationOutcome.Activated;
    }

    /// <summary>
    /// Sends a fresh activation e-mail when the address belongs to a pending account.
    /// Silent for unknown, active and reserved accounts so callers cannot probe which e-mails exist.
    /// </summary>
    public async Task ResendAsync(string? email, CancellationToken cancellationToken = default)
    {
        var address = email?.Trim() ?? "";
        if (address.Length == 0 || address.Length > SignUpService.MaxEmailLength
            || !MailAddress.TryCreate(address, out _))
            return;

        var user = await _users.FindByEmailAsync(address);
        if (user is null || user.EmailConfirmed || user.Reservado)
            return;

        await SendActivationAsync(user, cancellationToken);
    }

    internal async Task SendActivationAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!_limiter.TryAcquire(user.Email!))
        {
            _logger.LogWarning("Limite de e-mails de ativação atingido para o usuário {UserId}.", user.Id);
            return;
        }

        try
        {
            var token = await _users.GenerateEmailConfirmationTokenAsync(user);
            await _email.SendActivationAsync(user.Email!, user.Id, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar o e-mail de ativação para o usuário {UserId}.", user.Id);
        }
    }

    private async Task EnsureDefaultTiposAsync(string userId)
    {
        await using var db = new AppDbContext(_dbOptions, userId);
        var existing = await db.TiposInsumo.Select(t => t.Nome).ToListAsync();
        var missing = DefaultTipos
            .Where(nome => !existing.Contains(nome, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (missing.Count == 0)
            return;

        db.TiposInsumo.AddRange(missing.Select(nome => new TipoInsumo { Nome = nome }));
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A concurrent activation click already created them (unique index on user + name).
            _logger.LogInformation("Tipos padrão do usuário {UserId} já existiam.", userId);
        }
    }
}
