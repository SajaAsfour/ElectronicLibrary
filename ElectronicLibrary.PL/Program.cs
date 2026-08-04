using ElectronicLibrary.DAL.Seed;
using ElectronicLibrary.PL.Extensions;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabaseServices(builder.Configuration);

builder.Services.AddIdentityServices();

builder.Services.AddJwtAuthenticationServices(builder.Configuration);

builder.Services.AddAuthorizationPolicies();

builder.Services.AddApplicationServices();

builder.Services.AddFileStorageServices(builder.Configuration);

builder.Services.AddEmailServices(builder.Configuration);

builder.Services.AddLocalizationServices();

var app = builder.Build();

var localizationOptions =
    app.Services
        .GetRequiredService<
            IOptions<RequestLocalizationOptions>>()
        .Value;

app.UseRequestLocalization(localizationOptions);

app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    await DatabaseSeeder.SeedAsync(
        app.Services,
        app.Configuration);
}

app.Run();

public partial class Program;