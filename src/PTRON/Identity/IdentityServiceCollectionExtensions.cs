using Microsoft.AspNetCore.Components.Server.Circuits;
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
        services.AddSingleton<PasswordResetLimiter>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<LoginService>();
        services.AddScoped<SignUpService>();
        services.AddScoped<ChangePasswordService>();
        services.AddSingleton<SessionStampValidator>();
        services.AddScoped<CircuitHandler, SessionCircuitHandler>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedEmail = true;

                options.Tokens.PasswordResetTokenProvider = PasswordResetTokenProvider.ProviderName;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddTokenProvider<PasswordResetTokenProvider>(PasswordResetTokenProvider.ProviderName)
            .AddPasswordValidator<LetterPasswordValidator>()
            .AddUserValidator<ReservedUserValidator>();

        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));
        services.Configure<PasswordResetTokenProviderOptions>(o =>
        {
            o.Name = PasswordResetTokenProvider.ProviderName;
            o.TokenLifespan = TimeSpan.FromHours(1);
        });

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
