using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using MudBlazor;
using MudBlazor.Services;
using PTRON.Api;
using PTRON.Data;
using PTRON.Email;
using PTRON.Identity;
using PTRON.Services;

var builder = WebApplication.CreateBuilder(args);

// Deployment settings and secrets (SMTP, Leo password, ...) come from PTRON_* environment variables.
builder.Configuration.AddEnvironmentVariables(prefix: "PTRON_");

using var startupLogs = LoggerFactory.Create(l => l.AddSimpleConsole());
var startupLogger = startupLogs.CreateLogger("PTRON.Startup");

// Brazilian culture for currency/number/date formatting.
var ptBr = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        // Keep the session after the phone sleeps or the user switches apps.
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromHours(2);
        options.DisconnectedCircuitMaxRetained = 100;
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
    })
    .AddHubOptions(options =>
    {
        // A backgrounded phone stops sending pings. Wait longer before
        // treating that pause as a dead connection.
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        // Camera photos are streamed through the circuit. The 32 KB default
        // drops the connection before a phone picture finishes uploading.
        options.MaximumReceiveMessageSize = 2 * 1024 * 1024;
    });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnValidatePrincipal = SessionStampValidator.RejectIfStampMismatchAsync;
    });
// Not Secure.Always: TLS ends at the proxy, so Kestrel sees plain HTTP and the antiforgery
// system throws on every request that issues a token when it requires HTTPS.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ReservedUsers.PolicyName, policy =>
        policy.RequireAssertion(context => ReservedUsers.IsReservedPrincipal(context.User)));
});
builder.Services.AddMudServices(config =>
{
    // FlipAlways refits the menu on every viewport change. On a phone in
    // portrait the menu and the browser chrome resize each other in a loop.
    config.PopoverOptions.OverflowBehavior = OverflowBehavior.FlipOnOpen;
});
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<ApiAuthOptions>(builder.Configuration.GetSection(ApiAuthOptions.SectionName));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PTRON API",
        Version = "v1",
        Description = "REST API for the PTRON Android / external clients. Send the token from Api:Token as Bearer or X-Api-Key."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token from appsettings Api:Token. Example: ptron-dev-token-8f4c2a91",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
})
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
            ?? "Requisição inválida.";
        return new BadRequestObjectResult(new ErrorDto { Error = message! });
    };
});

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=ptron.db"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.Replace(ServiceDescriptor.Scoped<IDbContextFactory<AppDbContext>, TenantDbContextFactory>());
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

builder.Services.AddPtronDataProtection(builder.Configuration, builder.Environment, startupLogger);
builder.Services.AddPtronIdentity(builder.Configuration);
builder.Services.AddPtronEmail(builder.Configuration);

builder.Services.AddScoped<ImageUploadService>();
builder.Services.AddScoped<TipoInsumoService>();
builder.Services.AddScoped<InsumoService>();
builder.Services.AddScoped<EquipamentoService>();
builder.Services.AddScoped<EntradaEstoqueService>();
builder.Services.AddScoped<EntradaCartState>();
builder.Services.AddScoped<ProducaoService>();
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<SqlConsoleService>();

var app = builder.Build();
app.LogEmailStartup();

// Apply migrations / create the database on startup. Default types are created per user on activation.
using (var scope = app.Services.CreateScope())
{
    var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
    await using (var db = new AppDbContext(options, ReservedUsers.LeoId))
    {
        db.Database.Migrate();
    }

    await LeoBootstrap.EnsureAsync(
        scope.ServiceProvider,
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("PTRON.Leo"));

    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    Directory.CreateDirectory(Path.Combine(env.WebRootPath, "uploads"));
}

var supportedCultures = new[] { ptBr };
app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(ptBr),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    api => api.UseMiddleware<ApiTokenMiddleware>());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();
app.MapFallback("/api/{**slug}", () => Results.Json(
    new ErrorDto { Error = "Endpoint não encontrado." },
    statusCode: StatusCodes.Status404NotFound));
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
