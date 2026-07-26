using ElectronicLibrary.DAL.Seed;
using ElectronicLibrary.PL.Extensions;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabaseServices(builder.Configuration);

builder.Services.AddIdentityServices();

builder.Services.AddJwtAuthenticationServices(builder.Configuration);

builder.Services.AddApplicationServices();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DatabaseSeeder.SeedAsync(
    app.Services,
    app.Configuration);

app.Run();