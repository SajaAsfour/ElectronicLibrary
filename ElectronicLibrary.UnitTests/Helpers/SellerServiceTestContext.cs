using ElectronicLibrary.BLL.Interfaces.Sellers;
using ElectronicLibrary.BLL.Services.Sellers;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class SellerServiceTestContext
    : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public SellerServiceTestContext()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"SellerTests-{Guid.NewGuid()}"));

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        _serviceProvider =
            services.BuildServiceProvider();

        _scope = _serviceProvider.CreateScope();

        DbContext = _scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        UserManager = _scope.ServiceProvider
            .GetRequiredService<
                UserManager<ApplicationUser>>();

        CurrentUserService =
            new FakeCurrentUserService(
                "unit-test-current-user-id");

        SellerService = new SellerService(
            UserManager,
            CurrentUserService);

        SeedSellerRole();
    }

    public ApplicationDbContext DbContext { get; }

    public UserManager<ApplicationUser> UserManager
    {
        get;
    }

    public FakeCurrentUserService CurrentUserService
    {
        get;
    }

    public ISellerService SellerService { get; }

    public async Task<ApplicationUser> CreateUserAsync(
        string id,
        string userName,
        string fullName = "Test User",
        string? email = null,
        string? storeName = null,
        string? sellerBio = null,
        decimal? sellerRating = null,
        string? city = null,
        string? address = null,
        bool isSeller = false,
        bool isDeleted = false)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = userName,
            Email = email ??
                $"{userName}@example.com",
            EmailConfirmed = true,
            FullName = fullName,
            StoreName = storeName,
            SellerBio = sellerBio,
            SellerRating = sellerRating,
            City = city,
            Address = address
        };

        IdentityResult createResult =
            await UserManager.CreateAsync(user);

        EnsureIdentityResultSucceeded(
            createResult);

        if (isSeller)
        {
            IdentityResult roleResult =
                await UserManager.AddToRoleAsync(
                    user,
                    ApplicationRoles.Seller);

            EnsureIdentityResultSucceeded(
                roleResult);
        }

        if (isDeleted)
        {
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            IdentityResult updateResult =
                await UserManager.UpdateAsync(user);

            EnsureIdentityResultSucceeded(
                updateResult);
        }

        return user;
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.Database
            .EnsureDeletedAsync();

        _scope.Dispose();

        await _serviceProvider.DisposeAsync();
    }

    private void SeedSellerRole()
    {
        DbContext.Roles.Add(
            new IdentityRole
            {
                Id = "unit-test-seller-role-id",
                Name = ApplicationRoles.Seller,
                NormalizedName =
                    ApplicationRoles.Seller
                        .ToUpperInvariant(),
                ConcurrencyStamp =
                    Guid.NewGuid().ToString()
            });

        DbContext.SaveChanges();
    }

    private static void
        EnsureIdentityResultSucceeded(
            IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join(
            Environment.NewLine,
            result.Errors.Select(
                error =>
                    $"{error.Code}: " +
                    error.Description));

        throw new InvalidOperationException(
            errors);
    }
}
