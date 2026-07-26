using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace ElectronicLibrary.PL.Extensions;

public static class LocalizationExtensions
{
    public static IServiceCollection AddLocalizationServices(
        this IServiceCollection services)
    {
        services.AddLocalization();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ar")
            };

            options.DefaultRequestCulture =
                new RequestCulture("en");

            options.SupportedCultures =
                supportedCultures;

            options.SupportedUICultures =
                supportedCultures;

            options.ApplyCurrentCultureToResponseHeaders = true;

            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        return services;
    }
}