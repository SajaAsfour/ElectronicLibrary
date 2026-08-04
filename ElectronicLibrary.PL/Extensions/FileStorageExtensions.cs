using ElectronicLibrary.BLL.Interfaces.Storage;
using ElectronicLibrary.BLL.Options;
using ElectronicLibrary.PL.Services.Storage;

namespace ElectronicLibrary.PL.Extensions;

public static class FileStorageExtensions
{
    public static IServiceCollection AddFileStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(
            configuration.GetSection(
                FileStorageOptions.SectionName));

        services.AddScoped<
            IFileStorageService,
            LocalFileStorageService>();

        return services;
    }
}