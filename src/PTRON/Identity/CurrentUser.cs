using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PTRON.Identity;

/// <summary>
/// Reads the user id from the HTTP request (cookie or API token) and, on a Blazor
/// circuit where there is no HttpContext, from the authentication state.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationState;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider authenticationState)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationState = authenticationState;
    }

    public string UserId
    {
        get
        {
            var fromHttp = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(fromHttp))
            {
                return fromHttp;
            }

            try
            {
                var task = _authenticationState.GetAuthenticationStateAsync();
                if (!task.IsCompletedSuccessfully)
                {
                    return "";
                }

                return task.Result.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
