using ElectronicLibrary.DAL.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElectronicLibrary.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "ASPNETCORE_ENVIRONMENT",
            "Testing");

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=(localdb)\\MSSQLLocalDB;" +
            "Database=ElectronicLibraryIntegrationTests;" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True");

        Environment.SetEnvironmentVariable(
            "Jwt__SecretKey",
            "ElectronicLibraryIntegrationTestsSecureJwtKey2026MinimumThirtyTwoCharacters");

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer",
            "ElectronicLibrary.IntegrationTests");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience",
            "ElectronicLibrary.IntegrationTests");

        Environment.SetEnvironmentVariable(
            "Jwt__AccessTokenExpirationMinutes",
            "15");

        Environment.SetEnvironmentVariable(
            "Jwt__RefreshTokenExpirationDays",
            "7");

        Environment.SetEnvironmentVariable(
            "Email__Host",
            "localhost");

        Environment.SetEnvironmentVariable(
            "Email__Port",
            "25");

        Environment.SetEnvironmentVariable(
            "Email__UserName",
            "test");

        Environment.SetEnvironmentVariable(
            "Email__Password",
            "test");

        Environment.SetEnvironmentVariable(
            "Email__FromEmail",
            "tests@electroniclibrary.local");

        Environment.SetEnvironmentVariable(
            "Email__FromName",
            "Electronic Library Tests");

        Environment.SetEnvironmentVariable(
            "Email__EnableSsl",
            "false");
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(
     IHostBuilder builder)
    {
        IHost host = base.CreateHost(builder);

        using IServiceScope scope =
            host.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        return host;
    }
}