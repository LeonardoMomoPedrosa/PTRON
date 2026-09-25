using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PTRON.Api;

public sealed class ApiAuthOptions
{
    public const string SectionName = "Api";

    /// <summary>Shared token expected in Authorization: Bearer or X-Api-Key.</summary>
    public string Token { get; set; } = "ptron-dev-token-8f4c2a91";
}

public sealed class ApiTokenMiddleware
{
    public const string ApiKeyHeader = "X-Api-Key";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly string _token;

    public ApiTokenMiddleware(RequestDelegate next, IOptions<ApiAuthOptions> options)
    {
        _next = next;
        _token = options.Value.Token ?? string.Empty;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method) || TokenMatches(context))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new ErrorDto { Error = "Não autorizado." }, JsonOptions));
    }

    private bool TokenMatches(HttpContext context)
    {
        if (string.IsNullOrEmpty(_token))
        {
            return false;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var bearer = authorization["Bearer ".Length..].Trim();
            if (string.Equals(bearer, _token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        var apiKey = context.Request.Headers[ApiKeyHeader].ToString();
        return string.Equals(apiKey, _token, StringComparison.Ordinal);
    }
}
