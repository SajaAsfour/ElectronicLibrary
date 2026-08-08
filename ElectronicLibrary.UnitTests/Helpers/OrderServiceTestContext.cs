using ElectronicLibrary.BLL.Helpers.Marketplace;
using ElectronicLibrary.BLL.Interfaces.Orders;
using ElectronicLibrary.BLL.Services.Orders;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Models.Discounts;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.DAL.Models.Shopping;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class OrderServiceTestContext
    : IAsyncDisposable
{
    private readonly ServiceProvider
        _serviceProvider;

    private readonly IServiceScope _scope;

    public OrderServiceTestContext()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(
            options =>
                options
                    .UseInMemoryDatabase(
                        $"OrderTests-{Guid.NewGuid()}")
                    .ConfigureWarnings(
                        warnings =>
                            warnings.Ignore(
                                InMemoryEventId
                                    .TransactionIgnoredWarning)));

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

        OrderService =
            new OrderService(
                UnitOfWork,
                CurrentUserService,
                UserManager,
                DbContext);

        SeedRoles();
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

    public TestUnitOfWork
        UnitOfWork
    {
        get;
    }

    public FakeCurrentUserService
        CurrentUserService
    {
        get;
    }

    public IOrderService
        OrderService
    {
        get;
    }

    public async Task<ApplicationUser>
        CreateUserAsync(
            string id,
            string userName,
            string? storeName = null,
            bool isSeller = false,
            bool isAdmin = false,
            bool isDeleted = false)
    {
        var user =
            new ApplicationUser
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
            await UserManager.CreateAsync(
                user);

        EnsureIdentityResultSucceeded(
            createResult);

        if (isSeller)
        {
            IdentityResult sellerRoleResult =
                await UserManager.AddToRoleAsync(
                    user,
                    ApplicationRoles.Seller);

            EnsureIdentityResultSucceeded(
                sellerRoleResult);
        }

        if (isAdmin)
        {
            IdentityResult adminRoleResult =
                await UserManager.AddToRoleAsync(
                    user,
                    ApplicationRoles.Admin);

            EnsureIdentityResultSucceeded(
                adminRoleResult);
        }

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

    public async Task<Book>
        CreateBookAsync(
            string title =
                "Order Test Book",
            bool isDeleted = false)
    {
        var publisher =
            new Publisher
            {
                Name =
                    $"Publisher-" +
                    $"{Guid.NewGuid():N}"
            };

        var book =
            new Book
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
                ListingStatus.Active,
            bool isDeleted = false)
    {
        var listing =
            new Listing
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
                    : null,
                RowVersion = []
            };

        DbContext.Listings.Add(
            listing);

        await DbContext.SaveChangesAsync();

        return listing;
    }

    public async Task<Cart>
        CreateCartAsync(
            ApplicationUser user)
    {
        var cart =
            new Cart
            {
                UserId = user.Id,
                User = user,
                CreatedAt = DateTime.UtcNow
            };

        DbContext.Carts.Add(
            cart);

        await DbContext.SaveChangesAsync();

        return cart;
    }

    public async Task<CartItem>
        CreateCartItemAsync(
            Cart cart,
            Listing listing,
            int quantity = 1)
    {
        var cartItem =
            new CartItem
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

    public async Task<Coupon>
        CreateCouponAsync(
            string code = "BOOK10",
            decimal discountValue = 10m,
            string discountType =
                "Percentage",
            bool isActive = true,
            DateTime? startDate = null,
            DateTime? endDate = null)
    {
        var coupon =
            new Coupon
            {
                Code = code,
                DiscountValue =
                    discountValue,
                DiscountType =
                    discountType,
                IsActive = isActive,
                StartDate =
                    startDate ??
                    DateTime.UtcNow.AddDays(-1),
                EndDate =
                    endDate ??
                    DateTime.UtcNow.AddDays(1)
            };

        DbContext.Coupons.Add(
            coupon);

        await DbContext.SaveChangesAsync();

        return coupon;
    }

    public async Task<Order>
        CreateOrderAsync(
            ApplicationUser customer,
            OrderStatus status =
                OrderStatus.Pending,
            DateTime? orderDate = null,
            decimal subtotalAmount = 0m,
            decimal listingDiscountAmount = 0m,
            decimal couponDiscountAmount = 0m,
            Coupon? coupon = null)
    {
        decimal totalDiscountAmount =
            listingDiscountAmount +
            couponDiscountAmount;

        decimal totalAmount =
            Math.Max(
                0m,
                subtotalAmount -
                totalDiscountAmount);

        var order =
            new Order
            {
                OrderDate =
                    orderDate ??
                    DateTime.UtcNow,
                SubtotalAmount =
                    subtotalAmount,
                ListingDiscountAmount =
                    listingDiscountAmount,
                CouponDiscountAmount =
                    couponDiscountAmount,
                TotalDiscountAmount =
                    totalDiscountAmount,
                TotalAmount =
                    totalAmount,
                Status = status,
                UserId = customer.Id,
                User = customer,
                CouponId =
                    coupon?.CouponId,
                Coupon = coupon,
                CouponCodeSnapshot =
                    coupon?.Code,
                CouponDiscountTypeSnapshot =
                    coupon?.DiscountType,
                CouponDiscountValueSnapshot =
                    coupon?.DiscountValue
            };

        DbContext.Orders.Add(
            order);

        await DbContext.SaveChangesAsync();

        return order;
    }

    public async Task<OrderItem>
        CreateOrderItemAsync(
            Order order,
            Listing listing,
            int quantity = 1,
            OrderItemStatus status =
                OrderItemStatus.Pending)
    {
        ListingPriceBreakdown breakdown =
            ListingPriceCalculator.CalculateLine(
                listing.Price,
                listing.DiscountPercentage,
                quantity);

        var orderItem =
            new OrderItem
            {
                OrderId = order.OrderId,
                Order = order,
                ListingId =
                    listing.ListingId,
                Listing = listing,
                BookId = listing.BookId,
                SellerId =
                    listing.SellerId,
                BookTitleSnapshot =
                    listing.Book.Title,
                SellerStoreNameSnapshot =
                    listing.Seller.StoreName ??
                    listing.Seller.UserName ??
                    "Seller",
                FormatSnapshot =
                    listing.Format,
                ConditionSnapshot =
                    listing.Condition,
                Quantity = quantity,
                UnitPrice =
                    breakdown.UnitPrice,
                DiscountPercentage =
                    breakdown.DiscountPercentage,
                EffectiveUnitPrice =
                    breakdown.EffectiveUnitPrice,
                LineSubtotal =
                    breakdown.LineSubtotal,
                LineDiscount =
                    breakdown.LineDiscount,
                LineTotal =
                    breakdown.LineTotal,
                Status = status
            };

        DbContext.OrderItems.Add(
            orderItem);

        order.SubtotalAmount +=
            breakdown.LineSubtotal;

        order.ListingDiscountAmount +=
            breakdown.LineDiscount;

        order.TotalDiscountAmount =
            order.ListingDiscountAmount +
            order.CouponDiscountAmount;

        order.TotalAmount =
            Math.Max(
                0m,
                order.SubtotalAmount -
                order.TotalDiscountAmount);

        await DbContext.SaveChangesAsync();

        return orderItem;
    }

    public void ChangeCurrentUser(
        string userId)
    {
        CurrentUserService.UserId =
            userId;
    }

    public void ClearTracking()
    {
        DbContext.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.Database
            .EnsureDeletedAsync();

        _scope.Dispose();

        await _serviceProvider
            .DisposeAsync();
    }

    private void SeedRoles()
    {
        DbContext.Roles.AddRange(
            new IdentityRole
            {
                Id =
                    "unit-test-order-seller-role",
                Name =
                    ApplicationRoles.Seller,
                NormalizedName =
                    ApplicationRoles.Seller
                        .ToUpperInvariant(),
                ConcurrencyStamp =
                    Guid.NewGuid().ToString()
            },
            new IdentityRole
            {
                Id =
                    "unit-test-order-admin-role",
                Name =
                    ApplicationRoles.Admin,
                NormalizedName =
                    ApplicationRoles.Admin
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

        string errors =
            string.Join(
                Environment.NewLine,
                result.Errors.Select(
                    error =>
                        $"{error.Code}: " +
                        error.Description));

        throw new InvalidOperationException(
            errors);
    }
}
