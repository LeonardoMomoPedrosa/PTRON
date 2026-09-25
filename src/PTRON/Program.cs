using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MudBlazor.Services;
using PTRON.Api;
using PTRON.Data;
using PTRON.Services;

var builder = WebApplication.CreateBuilder(args);

// Brazilian culture for currency/number/date formatting.
var ptBr = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
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

// Apply migrations / create the database on startup, then seed if empty.
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();

    // Ensure upload folder exists.
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    Directory.CreateDirectory(Path.Combine(env.WebRootPath, "uploads"));

    await SeedData.EnsureSeededAsync(factory);
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
