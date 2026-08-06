using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.BLL.Interfaces.Shopping;
using ElectronicLibrary.DAL.DTOs.Requests.Carts;
using ElectronicLibrary.DAL.DTOs.Responses.Carts;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Shopping;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.BLL.Services.Shopping;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<CartResponse>
        GetCurrentUserCartAsync(
            CancellationToken cancellationToken = default)
    {
        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        Cart cart = await GetOrCreateCartAsync(
            user.Id,
            cancellationToken);

        return await BuildCartResponseAsync(
            cart,
            cancellationToken);
    }

    public async Task<CartResponse> AddCartItemAsync(
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateListingId(request.ListingId);
        ValidateQuantity(request.Quantity);

        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        CartListingSnapshot? listing =
            await GetListingSnapshotAsync(
                request.ListingId,
                cancellationToken);

        if (listing is null)
        {
            throw new KeyNotFoundException(
                "ListingNotFound");
        }

        ValidateListingAvailable(listing);

        Cart cart = await GetOrCreateCartAsync(
            user.Id,
            cancellationToken);

        CartItem? existingItem = await _unitOfWork
            .Repository<CartItem>()
            .Query()
            .FirstOrDefaultAsync(
                item =>
                    item.CartId == cart.CartId &&
                    item.ListingId ==
                        request.ListingId,
                cancellationToken);

        long combinedQuantity =
            (long)(existingItem?.Quantity ?? 0) +
            request.Quantity;

        if (combinedQuantity >
            listing.AvailableQuantity)
        {
            throw new InvalidOperationException(
                "InsufficientStock");
        }

        if (existingItem is null)
        {
            var cartItem = new CartItem
            {
                CartId = cart.CartId,
                ListingId = request.ListingId,
                Quantity = request.Quantity
            };

            await _unitOfWork
                .Repository<CartItem>()
                .AddAsync(
                    cartItem,
                    cancellationToken);
        }
        else
        {
            existingItem.Quantity =
                (int)combinedQuantity;

            _unitOfWork
                .Repository<CartItem>()
                .Update(existingItem);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await BuildCartResponseAsync(
            cart,
            cancellationToken);
    }

    public async Task<CartResponse>
        UpdateCartItemQuantityAsync(
            int listingId,
            UpdateCartItemQuantityRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateListingId(listingId);
        ValidateQuantity(request.Quantity);

        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        Cart? cart = await GetCartAsync(
            user.Id,
            cancellationToken);

        if (cart is null)
        {
            throw new KeyNotFoundException(
                "CartItemNotFound");
        }

        CartItem? cartItem = await _unitOfWork
            .Repository<CartItem>()
            .Query()
            .FirstOrDefaultAsync(
                item =>
                    item.CartId == cart.CartId &&
                    item.ListingId == listingId,
                cancellationToken);

        if (cartItem is null)
        {
            throw new KeyNotFoundException(
                "CartItemNotFound");
        }

        CartListingSnapshot? listing =
            await GetListingSnapshotAsync(
                listingId,
                cancellationToken);

        if (listing is null)
        {
            throw new KeyNotFoundException(
                "ListingNotFound");
        }

        ValidateListingAvailable(listing);

        if (request.Quantity >
            listing.AvailableQuantity)
        {
            throw new InvalidOperationException(
                "InsufficientStock");
        }

        cartItem.Quantity = request.Quantity;

        _unitOfWork
            .Repository<CartItem>()
            .Update(cartItem);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await BuildCartResponseAsync(
            cart,
            cancellationToken);
    }

    public async Task RemoveCartItemAsync(
        int listingId,
        CancellationToken cancellationToken = default)
    {
        ValidateListingId(listingId);

        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        Cart? cart = await GetCartAsync(
            user.Id,
            cancellationToken);

        if (cart is null)
        {
            throw new KeyNotFoundException(
                "CartItemNotFound");
        }

        CartItem? cartItem = await _unitOfWork
            .Repository<CartItem>()
            .Query()
            .FirstOrDefaultAsync(
                item =>
                    item.CartId == cart.CartId &&
                    item.ListingId == listingId,
                cancellationToken);

        if (cartItem is null)
        {
            throw new KeyNotFoundException(
                "CartItemNotFound");
        }

        _unitOfWork
            .Repository<CartItem>()
            .Delete(cartItem);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ClearCartAsync(
        CancellationToken cancellationToken = default)
    {
        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        Cart? cart = await GetCartAsync(
            user.Id,
            cancellationToken);

        if (cart is null)
        {
            return;
        }

        List<CartItem> cartItems =
            await _unitOfWork
                .Repository<CartItem>()
                .Query()
                .Where(item =>
                    item.CartId == cart.CartId)
                .ToListAsync(cancellationToken);

        if (cartItems.Count == 0)
        {
            return;
        }

        foreach (CartItem item in cartItems)
        {
            _unitOfWork
                .Repository<CartItem>()
                .Delete(item);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<ApplicationUser>
        GetCurrentUserAsync(
            CancellationToken cancellationToken)
    {
        string userId =
            _currentUserService.GetUserId();

        ApplicationUser? user =
            await _userManager
                .Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    currentUser =>
                        currentUser.Id == userId &&
                        !currentUser.IsDeleted,
                    cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "UserNotFound");
        }

        return user;
    }

    private async Task<Cart> GetOrCreateCartAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        Cart? cart = await GetCartAsync(
            userId,
            cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork
            .Repository<Cart>()
            .AddAsync(
                cart,
                cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return cart;
    }

    private async Task<Cart?> GetCartAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork
            .Repository<Cart>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                cart =>
                    cart.UserId == userId,
                cancellationToken);
    }

    private async Task<CartResponse>
        BuildCartResponseAsync(
            Cart cart,
            CancellationToken cancellationToken)
    {
        List<CartItem> cartItems =
            await _unitOfWork
                .Repository<CartItem>()
                .Query()
                .AsNoTracking()
                .Where(item =>
                    item.CartId == cart.CartId)
                .OrderBy(item =>
                    item.ListingId)
                .ToListAsync(cancellationToken);

        if (cartItems.Count == 0)
        {
            return new CartResponse
            {
                CartId = cart.CartId,
                UserId = cart.UserId
            };
        }

        int[] listingIds = cartItems
            .Select(item => item.ListingId)
            .Distinct()
            .ToArray();

        List<CartListingSnapshot> listings =
            await ProjectListingSnapshots(
                    _unitOfWork
                        .Repository<Listing>()
                        .Query()
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .Where(listing =>
                            listingIds.Contains(
                                listing.ListingId)))
                .ToListAsync(cancellationToken);

        Dictionary<int, CartListingSnapshot>
            listingsById = listings.ToDictionary(
                listing => listing.ListingId);

        List<CartItemResponse> responseItems =
            cartItems
                .Select(item =>
                {
                    listingsById.TryGetValue(
                        item.ListingId,
                        out CartListingSnapshot?
                            listing);

                    return CreateCartItemResponse(
                        item,
                        listing);
                })
                .ToList();

        return new CartResponse
        {
            CartId = cart.CartId,
            UserId = cart.UserId,
            Items = responseItems,
            TotalItems = responseItems.Sum(
                item => item.Quantity),
            Subtotal = responseItems.Sum(
                item => item.LineSubtotal),
            TotalDiscount = responseItems.Sum(
                item => item.LineDiscount),
            FinalTotal = responseItems.Sum(
                item => item.LineTotal)
        };
    }

    private async Task<CartListingSnapshot?>
        GetListingSnapshotAsync(
            int listingId,
            CancellationToken cancellationToken)
    {
        return await ProjectListingSnapshots(
                _unitOfWork
                    .Repository<Listing>()
                    .Query()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(listing =>
                        listing.ListingId ==
                        listingId))
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    private static IQueryable<CartListingSnapshot>
        ProjectListingSnapshots(
            IQueryable<Listing> query)
    {
        return query.Select(listing =>
            new CartListingSnapshot
            {
                ListingId = listing.ListingId,
                BookId = listing.BookId,
                BookTitle = listing.Book.Title,
                MainImageUrl =
                listing.Book.BookImages
                    .Where(image =>
                        image.IsMain)
                    .OrderBy(image =>
                        image.BookImageId)
                    .Select(image =>
                        image.ImageUrl)
                    .FirstOrDefault(),
                SellerId = listing.SellerId,
                StoreName =
                    listing.Seller.StoreName ??
                    string.Empty,
                UnitPrice = listing.Price,
                DiscountPercentage =
                    listing.DiscountPercentage,
                AvailableQuantity =
                    listing.Quantity,
                Format = listing.Format,
                Condition = listing.Condition,
                Status = listing.Status,
                ListingIsDeleted =
                    listing.IsDeleted,
                BookIsDeleted =
                    listing.Book.IsDeleted
            });
    }

    private static CartItemResponse
        CreateCartItemResponse(
            CartItem cartItem,
            CartListingSnapshot? listing)
    {
        if (listing is null)
        {
            return new CartItemResponse
            {
                ListingId = cartItem.ListingId,
                Quantity = cartItem.Quantity,
                IsAvailable = false,
                AvailabilityMessage =
                    "ListingNotAvailable"
            };
        }

        decimal effectiveUnitPrice =
            listing.UnitPrice -
            listing.UnitPrice *
            listing.DiscountPercentage /
            100m;

        decimal lineSubtotal =
            listing.UnitPrice *
            cartItem.Quantity;

        decimal lineTotal =
            effectiveUnitPrice *
            cartItem.Quantity;

        decimal lineDiscount =
            lineSubtotal -
            lineTotal;

        string? availabilityMessage =
            GetAvailabilityMessage(
                cartItem,
                listing);

        return new CartItemResponse
        {
            ListingId = listing.ListingId,
            BookId = listing.BookId,
            BookTitle = listing.BookTitle,
            MainImageUrl = listing.MainImageUrl,
            SellerId = listing.SellerId,
            StoreName = listing.StoreName,
            Quantity = cartItem.Quantity,
            AvailableQuantity =
                Math.Max(
                    listing.AvailableQuantity,
                    0),
            UnitPrice = listing.UnitPrice,
            DiscountPercentage =
                listing.DiscountPercentage,
            EffectiveUnitPrice =
                effectiveUnitPrice,
            LineSubtotal = lineSubtotal,
            LineDiscount = lineDiscount,
            LineTotal = lineTotal,
            Format = listing.Format,
            Condition = listing.Condition,
            ListingStatus = listing.Status,
            IsAvailable =
                availabilityMessage is null,
            AvailabilityMessage =
                availabilityMessage
        };
    }

    private static string? GetAvailabilityMessage(
        CartItem cartItem,
        CartListingSnapshot listing)
    {
        if (listing.ListingIsDeleted)
        {
            return "ListingNotAvailable";
        }

        if (listing.BookIsDeleted)
        {
            return "ListingBookNotAvailable";
        }

        if (listing.Status ==
                ListingStatus.OutOfStock ||
            listing.AvailableQuantity <= 0)
        {
            return "ListingOutOfStock";
        }

        if (listing.Status !=
            ListingStatus.Active)
        {
            return "ListingNotAvailable";
        }

        if (cartItem.Quantity >
            listing.AvailableQuantity)
        {
            return "CartItemQuantityExceedsStock";
        }

        return null;
    }

    private static void ValidateListingAvailable(
        CartListingSnapshot listing)
    {
        if (listing.ListingIsDeleted ||
            listing.BookIsDeleted ||
            listing.Status !=
                ListingStatus.Active ||
            listing.AvailableQuantity <= 0)
        {
            throw new InvalidOperationException(
                "ListingNotAvailable");
        }
    }

    private static void ValidateListingId(
        int listingId)
    {
        if (listingId <= 0)
        {
            throw new InvalidOperationException(
                "ListingIdMustBeGreaterThanZero");
        }
    }

    private static void ValidateQuantity(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException(
                "CartQuantityMustBeGreaterThanZero");
        }
    }

    private sealed class CartListingSnapshot
    {
        public int ListingId { get; set; }

        public int BookId { get; set; }

        public string BookTitle { get; set; }
            = null!;

        public string? MainImageUrl { get; set; }

        public string SellerId { get; set; }
            = null!;

        public string StoreName { get; set; }
            = null!;

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercentage {
            get;
            set;
        }

        public int AvailableQuantity {
            get;
            set;
        }

        public BookFormat Format { get; set; }

        public BookCondition? Condition {
            get;
            set;
        }

        public ListingStatus Status { get; set; }

        public bool ListingIsDeleted {
            get;
            set;
        }

        public bool BookIsDeleted { get; set; }
    }
}
