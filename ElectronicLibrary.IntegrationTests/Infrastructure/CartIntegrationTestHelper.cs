using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Shopping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.IntegrationTests.Infrastructure;

public static class CartIntegrationTestHelper
{
    public static async Task<TestCartMarketplaceContext>
        CreateMarketplaceContextAsync(
            CustomWebApplicationFactory factory,
            decimal price = 100m,
            int quantity = 5,
            decimal discountPercentage = 0m,
            ListingStatus status =
                ListingStatus.Active,
            bool listingIsDeleted = false,
            bool bookIsDeleted = false)
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(factory);

        ListingIntegrationTestHelper.TestUserContext
            seller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(
                        factory,
                        $"Cart Store-{Guid.NewGuid():N}");

        ListingIntegrationTestHelper.TestBookContext
            book =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(
                        factory,
                        isDeleted: bookIsDeleted);

        ListingIntegrationTestHelper.TestListingContext
            listing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        factory,
                        seller.UserId,
                        book.BookId,
                        price: price,
                        quantity: quantity,
                        discountPercentage:
                            discountPercentage,
                        status: status,
                        isDeleted: listingIsDeleted);

        return new TestCartMarketplaceContext(
            customer,
            seller,
            book,
            listing);
    }

    public static async Task<TestCartContext>
        CreateCartAsync(
            CustomWebApplicationFactory factory,
            string userId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        var cart = new Cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Carts.Add(cart);

        await dbContext.SaveChangesAsync();

        return new TestCartContext(
            cart.CartId,
            cart.UserId);
    }

    public static async Task<TestCartItemContext>
        CreateCartItemAsync(
            CustomWebApplicationFactory factory,
            int cartId,
            int listingId,
            int quantity = 1)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        var cartItem = new CartItem
        {
            CartId = cartId,
            ListingId = listingId,
            Quantity = quantity
        };

        dbContext.CartItems.Add(cartItem);

        await dbContext.SaveChangesAsync();

        return new TestCartItemContext(
            cartItem.CartId,
            cartItem.ListingId,
            cartItem.Quantity);
    }

    public static async Task<Cart?>
        GetCartByUserIdAsync(
            CustomWebApplicationFactory factory,
            string userId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.Carts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                cart =>
                    cart.UserId == userId);
    }

    public static async Task<CartItem?>
        GetCartItemAsync(
            CustomWebApplicationFactory factory,
            int cartId,
            int listingId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.CartItems
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.CartId == cartId &&
                    item.ListingId == listingId);
    }

    public static async Task<int>
        GetCartCountForUserAsync(
            CustomWebApplicationFactory factory,
            string userId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.Carts
            .AsNoTracking()
            .CountAsync(
                cart =>
                    cart.UserId == userId);
    }

    public static async Task<int>
        GetCartItemCountForUserAsync(
            CustomWebApplicationFactory factory,
            string userId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.CartItems
            .AsNoTracking()
            .CountAsync(
                item =>
                    item.Cart.UserId ==
                    userId);
    }

    public static async Task<List<CartItem>>
        GetCartItemsForUserAsync(
            CustomWebApplicationFactory factory,
            string userId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.CartItems
            .AsNoTracking()
            .Where(item =>
                item.Cart.UserId == userId)
            .OrderBy(item =>
                item.ListingId)
            .ToListAsync();
    }

    public sealed record
        TestCartMarketplaceContext(
            ListingIntegrationTestHelper
                .TestUserContext Customer,
            ListingIntegrationTestHelper
                .TestUserContext Seller,
            ListingIntegrationTestHelper
                .TestBookContext Book,
            ListingIntegrationTestHelper
                .TestListingContext Listing);

    public sealed record TestCartContext(
        int CartId,
        string UserId);

    public sealed record TestCartItemContext(
        int CartId,
        int ListingId,
        int Quantity);
}
