using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PTRON.Data;
using PTRON.Models;

namespace PTRON.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddPtronIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LeoOptions>(configuration.GetSection(LeoOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ActivationEmailLimiter>();
        services.AddScoped<ActivationService>();
        services.AddScoped<SignUpService>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<LetterPasswordValidator>()
            .AddUserValidator<ReservedUserValidator>();

        // Activation links stay valid for 24h. Password reset gets its own 1h provider in E8-S7.
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));

        return services;
    }

    /// <summary>
    /// Persists Data Protection keys outside the deploy folder so cookies and activation /
    /// reset links survive restarts and deploys. Configure with PTRON_DataProtection__KeysPath.
    /// </summary>
    public static IServiceCollection AddPtronDataProtection(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment, ILogger logger)
    {
        var builder = services.AddDataProtection().SetApplicationName("PTRON");

        var configured = configuration["DataProtection:KeysPath"];
        var path = !string.IsNullOrWhiteSpace(configured)
            ? configured
            : environment.IsDevelopment()
                ? Path.Combine(environment.ContentRootPath, "App_Data", "keys")
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "ptron-data", "keys"));

        try
        {
            Directory.CreateDirectory(path);
            builder.PersistKeysToFileSystem(new DirectoryInfo(path));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Não foi possível usar a pasta de chaves {Path}. Cookies e links de e-mail serão invalidados a cada reinício.",
                path);
        }

        return services;
    }
}
