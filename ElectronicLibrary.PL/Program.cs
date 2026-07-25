using ElectronicLibrary.DAL.Seed;
using ElectronicLibrary.PL.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabaseServices(builder.Configuration);

builder.Services.AddIdentityServices();

builder.Services.AddJwtAuthenticationServices(builder.Configuration);

builder.Services.AddApplicationServices();

var app = builder.Build();


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DatabaseSeeder.SeedAsync(
    app.Services,
    app.Configuration);

app.Run();