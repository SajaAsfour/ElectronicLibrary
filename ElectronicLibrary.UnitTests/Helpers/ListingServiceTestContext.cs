using ElectronicLibrary.BLL.Interfaces.Marketplace;
using ElectronicLibrary.BLL.Services.Marketplace;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class ListingServiceTestContext
    : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public ListingServiceTestContext()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"ListingTests-{Guid.NewGuid()}"));

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        _serviceProvider =
            services.BuildServiceProvider();

        _scope =
            _serviceProvider.CreateScope();

        DbContext = _scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        UserManager = _scope.ServiceProvider
            .GetRequiredService<
                UserManager<ApplicationUser>>();

        UnitOfWork =
            new TestUnitOfWork(DbContext);

        CurrentUserService =
            new FakeCurrentUserService(
                "unit-test-seller-id");

        ListingService =
            new ListingService(
                UnitOfWork,
                CurrentUserService,
                UserManager);

        SeedSellerRole();
    }

    public ApplicationDbContext DbContext { get; }

    public UserManager<ApplicationUser>
        UserManager
    { get; }

    public TestUnitOfWork UnitOfWork { get; }

    public FakeCurrentUserService
        CurrentUserService
    { get; }

    public IListingService ListingService { get; }

    public async Task<ApplicationUser>
        CreateUserAsync(
            string id,
            string userName,
            string? storeName = null,
            bool isSeller = false,
            bool isDeleted = false)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = userName,
            Email = $"{userName}@example.com",
            EmailConfirmed = true,
            FullName = $"{userName} Full Name",
            StoreName = storeName
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

    public async Task<Book> CreateBookAsync(
        string title = "Listing Test Book",
        bool isDeleted = false)
    {
        var publisher = new Publisher
        {
            Name =
                $"Publisher-{Guid.NewGuid():N}"
        };

        var book = new Book
        {
            Title = title,
            Language = "English",
            Publisher = publisher,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted
                ? DateTime.UtcNow
                : null
        };

        DbContext.AddRange(
            publisher,
            book);

        await DbContext.SaveChangesAsync();

        return book;
    }

    public async Task<Listing>
        CreateListingAsync(
            Book book,
            ApplicationUser seller,
            decimal price = 100m,
            int quantity = 5,
            decimal discountPercentage = 0m,
            BookFormat format =
                BookFormat.Physical,
            BookCondition? condition =
                BookCondition.New,
            ListingStatus status =
                ListingStatus.Draft,
            bool isDeleted = false,
            DateTime? createdAt = null)
    {
        var listing = new Listing
        {
            BookId = book.BookId,
            Book = book,
            SellerId = seller.Id,
            Seller = seller,
            Price = price,
            Quantity = quantity,
            DiscountPercentage =
                discountPercentage,
            Format = format,
            Condition = condition,
            Status = status,
            CreatedAt =
                createdAt ?? DateTime.UtcNow,
            CreatedById = seller.Id,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted
                ? DateTime.UtcNow
                : null,
            DeletedById = isDeleted
                ? seller.Id
                : null
        };

        DbContext.Listings.Add(listing);

        await DbContext.SaveChangesAsync();

        return listing;
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
                Id =
                    "unit-test-listing-seller-role",
                Name =
                    ApplicationRoles.Seller,
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