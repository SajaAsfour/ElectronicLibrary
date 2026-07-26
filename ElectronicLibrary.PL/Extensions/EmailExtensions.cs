using ElectronicLibrary.BLL.Interfaces.Email;
using ElectronicLibrary.BLL.Options;
using ElectronicLibrary.BLL.Services.Email;

namespace ElectronicLibrary.PL.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var emailSection = configuration.GetSection(
            EmailOptions.SectionName);

        services
            .AddOptions<EmailOptions>()
            .Bind(emailSection)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host),
                "Email Host is missing.")
            .Validate(
                options => options.Port > 0,
                "Email Port must be greater than zero.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.UserName),
                "Email UserName is missing.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Password),
                "Email Password is missing.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FromEmail),
                "Email FromEmail is missing.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FromName),
                "Email FromName is missing.")
            .Validate(
                options => Uri.TryCreate(
                    options.ConfirmationUrl,
                    UriKind.Absolute,
                    out _),
                "Email ConfirmationUrl must be an absolute URL.")
            .ValidateOnStart();

        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}