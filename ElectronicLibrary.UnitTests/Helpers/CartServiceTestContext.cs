using ElectronicLibrary.BLL.Interfaces.Shopping;
using ElectronicLibrary.BLL.Services.Shopping;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Shopping;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class CartServiceTestContext
    : IAsyncDisposable
{
    private readonly ServiceProvider
        _serviceProvider;

    private readonly IServiceScope _scope;

    public CartServiceTestContext()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"CartTests-{Guid.NewGuid()}"));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<
                ApplicationDbContext>();

        _serviceProvider =
            services.BuildServiceProvider();

        _scope =
            _serviceProvider.CreateScope();

        DbContext = _scope.ServiceProvider
            .GetRequiredService<
                ApplicationDbContext>();

        UserManager = _scope.ServiceProvider
            .GetRequiredService<
                UserManager<ApplicationUser>>();

        UnitOfWork =
            new TestUnitOfWork(DbContext);

        CurrentUserService =
            new FakeCurrentUserService(
                "unit-test-customer-id");

        CartService =
            new CartService(
                UnitOfWork,
                CurrentUserService,
                UserManager);
    }

    public ApplicationDbContext
        DbContext
    {
        get;
    }

    public UserManager<ApplicationUser>
        UserManager
    {
        get;
    }

    public TestUnitOfWork UnitOfWork
    {
        get;
    }

    public FakeCurrentUserService
        CurrentUserService
    {
        get;
    }

    public ICartService CartService
    {
        get;
    }

    public async Task<ApplicationUser>
        CreateUserAsync(
            string id,
            string userName,
            bool isDeleted = false,
            string? storeName = null)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = userName,
            Email =
                $"{userName}@example.com",
            EmailConfirmed = true,
            FullName =
                $"{userName} Full Name",
            StoreName = storeName
        };

        IdentityResult createResult =
            await UserManager.CreateAsync(user);

        EnsureIdentityResultSucceeded(
            createResult);

        if (isDeleted)
        {
            user.IsDeleted = true;
            user.DeletedAt =
                DateTime.UtcNow;

            IdentityResult updateResult =
                await UserManager.UpdateAsync(
                    user);

            EnsureIdentityResultSucceeded(
                updateResult);
        }

        return user;
    }

    public async Task<Book> CreateBookAsync(
        string title = "Cart Test Book",
        bool isDeleted = false,
        string? mainImageUrl =
            "/uploads/books/cart-test.jpg")
    {
        var publisher = new Publisher
        {
            Name =
                $"Publisher-" +
                $"{Guid.NewGuid():N}"
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

        if (!string.IsNullOrWhiteSpace(
                mainImageUrl))
        {
            var bookImage = new BookImage
            {
                BookId = book.BookId,
                Book = book,
                ImageUrl = mainImageUrl,
                IsMain = true
            };

            DbContext.BookImages.Add(
                bookImage);

            await DbContext.SaveChangesAsync();
        }

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
                ListingStatus.Active,
            bool isDeleted = false)
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
            CreatedAt = DateTime.UtcNow,
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

    public async Task<Cart> CreateCartAsync(
        ApplicationUser user)
    {
        var cart = new Cart
        {
            UserId = user.Id,
            User = user,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Carts.Add(cart);

        await DbContext.SaveChangesAsync();

        return cart;
    }

    public async Task<CartItem>
        CreateCartItemAsync(
            Cart cart,
            Listing listing,
            int quantity = 1)
    {
        var cartItem = new CartItem
        {
            CartId = cart.CartId,
            Cart = cart,
            ListingId =
                listing.ListingId,
            Listing = listing,
            Quantity = quantity
        };

        DbContext.CartItems.Add(
            cartItem);

        await DbContext.SaveChangesAsync();

        return cartItem;
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.Database
            .EnsureDeletedAsync();

        _scope.Dispose();

        await _serviceProvider
            .DisposeAsync();
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
