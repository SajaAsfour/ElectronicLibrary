using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.IntegrationTests.Infrastructure;

public static class ListingIntegrationTestHelper
{
    private const string TestPassword =
        "ListingIntegrationTest@123";

    public static async Task<TestUserContext>
        CreateAuthenticatedUserAsync(
            CustomWebApplicationFactory factory,
            string role,
            string? storeName = null)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        IServiceProvider services =
            scope.ServiceProvider;

        RoleManager<IdentityRole> roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole>>();

        UserManager<ApplicationUser> userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        ITokenService tokenService =
            services.GetRequiredService<
                ITokenService>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            IdentityResult roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(role));

            EnsureIdentityResultSucceeded(
                roleResult);
        }

        string suffix =
            Guid.NewGuid().ToString("N");

        string email =
            $"{role.ToLowerInvariant()}-" +
            $"{suffix}@listingtests.local";

        var user = new ApplicationUser
        {
            FullName =
                $"{role} Listing Test User",
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            StoreName = storeName
        };

        IdentityResult createResult =
            await userManager.CreateAsync(
                user,
                TestPassword);

        EnsureIdentityResultSucceeded(
            createResult);

        IdentityResult roleAssignmentResult =
            await userManager.AddToRoleAsync(
                user,
                role);

        EnsureIdentityResultSucceeded(
            roleAssignmentResult);

        string accessToken =
            await tokenService
                .CreateAccessTokenAsync(user);

        return new TestUserContext(
            user.Id,
            accessToken,
            user.StoreName);
    }

    public static async Task<TestUserContext>
        CreateSellerAsync(
            CustomWebApplicationFactory factory,
            string? storeName = null)
    {
        string resolvedStoreName =
            storeName ??
            $"Store-{Guid.NewGuid():N}";

        return await CreateAuthenticatedUserAsync(
            factory,
            ApplicationRoles.Seller,
            resolvedStoreName);
    }

    public static async Task<TestUserContext>
        CreateCustomerAsync(
            CustomWebApplicationFactory factory)
    {
        return await CreateAuthenticatedUserAsync(
            factory,
            ApplicationRoles.Customer);
    }

    public static async Task<TestBookContext>
        CreateBookAsync(
            CustomWebApplicationFactory factory,
            bool isDeleted = false)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        string suffix =
            Guid.NewGuid().ToString("N");

        var publisher = new Publisher
        {
            Name =
                $"Listing Publisher-{suffix}",
            Website =
                "https://publisher.example.com",
            IsDeleted = false
        };

        var book = new Book
        {
            Title =
                $"Listing Book-{suffix}",
            Language = "English",
            Publisher = publisher,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted
                ? DateTime.UtcNow
                : null
        };

        dbContext.Publishers.Add(publisher);
        dbContext.Books.Add(book);

        await dbContext.SaveChangesAsync();

        return new TestBookContext(
            book.BookId,
            book.Title);
    }

    public static async Task<TestListingContext>
        CreateListingAsync(
            CustomWebApplicationFactory factory,
            string sellerId,
            int bookId,
            decimal price = 100m,
            int quantity = 5,
            decimal discountPercentage = 0m,
            BookFormat format =
                BookFormat.Physical,
            BookCondition? condition =
                BookCondition.New,
            ListingStatus status =
                ListingStatus.Draft,
            bool isDeleted = false)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        var listing = new Listing
        {
            SellerId = sellerId,
            BookId = bookId,
            Price = price,
            Quantity = quantity,
            DiscountPercentage =
                discountPercentage,
            Format = format,
            Condition = condition,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CreatedById = sellerId,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted
                ? DateTime.UtcNow
                : null,
            DeletedById = isDeleted
                ? sellerId
                : null
        };

        dbContext.Listings.Add(listing);

        await dbContext.SaveChangesAsync();

        return new TestListingContext(
            listing.ListingId,
            listing.BookId,
            listing.SellerId);
    }

    public static async Task<Listing>
        GetListingIgnoringFiltersAsync(
            CustomWebApplicationFactory factory,
            int listingId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.Listings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(listing =>
                listing.ListingId ==
                listingId);
    }

    public static async Task<int>
        GetListingCountIgnoringFiltersAsync(
            CustomWebApplicationFactory factory,
            int? bookId = null,
            string? sellerId = null)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        IQueryable<Listing> query =
            dbContext.Listings
                .IgnoreQueryFilters()
                .AsNoTracking();

        if (bookId.HasValue)
        {
            query = query.Where(listing =>
                listing.BookId ==
                bookId.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                sellerId))
        {
            query = query.Where(listing =>
                listing.SellerId ==
                sellerId);
        }

        return await query.CountAsync();
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
            result.Errors.Select(error =>
                $"{error.Code}: " +
                error.Description));

        throw new InvalidOperationException(
            errors);
    }

    public sealed record TestUserContext(
        string UserId,
        string AccessToken,
        string? StoreName);

    public sealed record TestBookContext(
        int BookId,
        string Title);

    public sealed record TestListingContext(
        int ListingId,
        int BookId,
        string SellerId);
}