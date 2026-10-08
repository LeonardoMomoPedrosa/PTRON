using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace PTRON.Identity;

/// <summary>
/// An open Blazor circuit keeps the principal it started with. After a password change that principal
/// still has the old stamp, so the next interaction sends the browser through logout.
/// </summary>
public sealed class SessionCircuitHandler : CircuitHandler
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SessionCircuitHandler> _logger;
    private int _loggingOut;

    public SessionCircuitHandler(IServiceProvider services, ILogger<SessionCircuitHandler> logger)
    {
        _services = services;
        _logger = logger;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            if (Volatile.Read(ref _loggingOut) == 1)
                return;

            var user = (await _services.GetRequiredService<AuthenticationStateProvider>()
                .GetAuthenticationStateAsync()).User;
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                await next(context);
                return;
            }

            var validator = _services.GetRequiredService<SessionStampValidator>();
            bool valid;
            try
            {
                valid = await validator.IsValidAsync(
                    userId, user.FindFirstValue(SessionStampValidator.StampClaimType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Não foi possível validar a sessão aberta.");
                await next(context);
                return;
            }

            if (valid)
            {
                await next(context);
                return;
            }

            if (Interlocked.Exchange(ref _loggingOut, 1) == 0)
            {
                _logger.LogInformation("Encerrando sessão antiga do usuário {UserId}.", userId);
                _services.GetRequiredService<NavigationManager>()
                    .NavigateTo("/account/logout", forceLoad: true);
            }
        };
    }
}
