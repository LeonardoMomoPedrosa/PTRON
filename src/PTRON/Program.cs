using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
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

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=ptron.db"));

builder.Services.AddScoped<ImageUploadService>();
builder.Services.AddScoped<TipoInsumoService>();
builder.Services.AddScoped<InsumoService>();
builder.Services.AddScoped<EquipamentoService>();
builder.Services.AddScoped<EntradaEstoqueService>();
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

app.MapHealthChecks("/health");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
