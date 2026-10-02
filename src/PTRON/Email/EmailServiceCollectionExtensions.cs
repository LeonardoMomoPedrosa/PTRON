namespace PTRON.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddPtronEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.AddSingleton<EmailComposer>();

        var smtp = configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
        if (smtp.IsConfigured)
        {
            services.AddSingleton<IAppEmailSender, SmtpAppEmailSender>();
        }
        else
        {
            services.AddSingleton<IAppEmailSender, LoggingAppEmailSender>();
        }

        return services;
    }

    public static void LogEmailStartup(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PTRON.Email");
        var smtp = app.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
        if (!smtp.IsConfigured)
        {
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("SMTP não configurado. E-mails serão gravados no log.");
            }
            else
            {
                logger.LogWarning(
                    "SMTP não configurado (PTRON_Smtp__Host e PTRON_Smtp__FromAddress). E-mails de ativação e senha não serão enviados.");
            }
        }
        else if (!SmtpAppEmailSender.TryParseSecurity(smtp.SecureSocketOptions, out _))
        {
            logger.LogWarning(
                "PTRON_Smtp__SecureSocketOptions inválido ({Value}). Use StartTls, SslOnConnect, None ou StartTlsWhenAvailable.",
                smtp.SecureSocketOptions);
        }

        if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(app.Configuration["App:PublicBaseUrl"]))
        {
            logger.LogWarning(
                "PTRON_App__PublicBaseUrl não definido. Os links dos e-mails ficarão sem o endereço do site.");
        }
    }
}
