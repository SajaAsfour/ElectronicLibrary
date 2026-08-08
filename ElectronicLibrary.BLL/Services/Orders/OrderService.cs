using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Helpers.Marketplace;
using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.BLL.Interfaces.Orders;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Discounts;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.DAL.Models.Shopping;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ElectronicLibrary.BLL.Services.Orders;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService
        _currentUserService;
    private readonly UserManager<ApplicationUser>
        _userManager;
    private readonly ApplicationDbContext _dbContext;

    public OrderService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<OrderDetailsResponse>
        CheckoutAsync(
            CheckoutRequest request,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        try
        {
            Cart? cart = await _unitOfWork
                .Repository<Cart>()
                .Query()
                .FirstOrDefaultAsync(
                    currentCart =>
                        currentCart.UserId ==
                        user.Id,
                    cancellationToken);

            if (cart is null)
            {
                throw new KeyNotFoundException(
                    "CartNotFound");
            }

            List<CartItem> cartItems =
                await _unitOfWork
                    .Repository<CartItem>()
                    .Query()
                    .Where(item =>
                        item.CartId ==
                        cart.CartId)
                    .OrderBy(item =>
                        item.ListingId)
                    .ToListAsync(
                        cancellationToken);

            if (cartItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "CartIsEmpty");
            }

            int[] listingIds = cartItems
                .Select(item =>
                    item.ListingId)
                .Distinct()
                .ToArray();

            List<Listing> listings =
                await _unitOfWork
                    .Repository<Listing>()
                    .Query()
                    .IgnoreQueryFilters()
                    .Include(listing =>
                        listing.Book)
                    .Include(listing =>
                        listing.Seller)
                    .Where(listing =>
                        listingIds.Contains(
                            listing.ListingId))
                    .ToListAsync(
                        cancellationToken);

            Dictionary<int, Listing> listingsById =
                listings.ToDictionary(
                    listing =>
                        listing.ListingId);

            DateTime now = DateTime.UtcNow;

            List<OrderItem> orderItems = [];

            foreach (CartItem cartItem in cartItems)
            {
                if (!listingsById.TryGetValue(
                        cartItem.ListingId,
                        out Listing? listing))
                {
                    throw new KeyNotFoundException(
                        "ListingNotFound");
                }

                ValidateListingForCheckout(
                    listing,
                    cartItem.Quantity);

                ListingPriceBreakdown price =
                    ListingPriceCalculator
                        .CalculateLine(
                            listing.Price,
                            listing
                                .DiscountPercentage,
                            cartItem.Quantity);

                orderItems.Add(
                    new OrderItem
                    {
                        ListingId =
                            listing.ListingId,
                        BookId =
                            listing.BookId,
                        SellerId =
                            listing.SellerId,
                        BookTitleSnapshot =
                            listing.Book.Title,
                        SellerStoreNameSnapshot =
                            listing.Seller
                                .StoreName ??
                            string.Empty,
                        FormatSnapshot =
                            listing.Format,
                        ConditionSnapshot =
                            listing.Condition,
                        Quantity =
                            cartItem.Quantity,
                        UnitPrice =
                            price.UnitPrice,
                        DiscountPercentage =
                            price
                                .DiscountPercentage,
                        EffectiveUnitPrice =
                            price
                                .EffectiveUnitPrice,
                        LineSubtotal =
                            price.LineSubtotal,
                        LineDiscount =
                            price.LineDiscount,
                        LineTotal =
                            price.LineTotal,
                        Status =
                            OrderItemStatus.Pending
                    });

                listing.Quantity -=
                    cartItem.Quantity;

                if (listing.Quantity == 0)
                {
                    listing.Status =
                        ListingStatus.OutOfStock;
                }

                listing.UpdatedAt = now;
                listing.UpdatedById = user.Id;
            }

            decimal subtotalAmount =
                orderItems.Sum(item =>
                    item.LineSubtotal);

            decimal listingDiscountAmount =
                orderItems.Sum(item =>
                    item.LineDiscount);

            decimal amountAfterListingDiscount =
                orderItems.Sum(item =>
                    item.LineTotal);

            string? normalizedCouponCode =
                NormalizeCouponCode(
                    request.CouponCode);

            Coupon? coupon = null;
            decimal couponDiscountAmount = 0m;

            if (normalizedCouponCode is not null)
            {
                coupon =
                    await GetValidCouponAsync(
                        normalizedCouponCode,
                        now,
                        cancellationToken);

                couponDiscountAmount =
                    CalculateCouponDiscount(
                        coupon,
                        amountAfterListingDiscount);
            }

            couponDiscountAmount = Math.Min(
                couponDiscountAmount,
                amountAfterListingDiscount);

            decimal totalDiscountAmount =
                listingDiscountAmount +
                couponDiscountAmount;

            decimal totalAmount =
                amountAfterListingDiscount -
                couponDiscountAmount;

            if (totalAmount < 0m)
            {
                totalAmount = 0m;
            }

            var order = new Order
            {
                OrderDate = now,
                UserId = user.Id,
                CouponId = coupon?.CouponId,
                CouponCodeSnapshot =
                    coupon?.Code,
                CouponDiscountTypeSnapshot =
                    coupon?.DiscountType,
                CouponDiscountValueSnapshot =
                    coupon?.DiscountValue,
                SubtotalAmount =
                    subtotalAmount,
                ListingDiscountAmount =
                    listingDiscountAmount,
                CouponDiscountAmount =
                    couponDiscountAmount,
                TotalDiscountAmount =
                    totalDiscountAmount,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending,
                OrderItems = orderItems
            };

            await _unitOfWork
                .Repository<Order>()
                .AddAsync(
                    order,
                    cancellationToken);

            foreach (CartItem cartItem in
                     cartItems)
            {
                _unitOfWork
                    .Repository<CartItem>()
                    .Delete(cartItem);
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return MapToOrderDetailsResponse(
                order);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw new ConflictException(
                "CheckoutConcurrencyConflict");
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    public async Task<
        PagedResponse<OrderSummaryResponse>>
    GetCurrentUserOrdersAsync(
        OrderFilterRequest request,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        IQueryable<Order> query = _unitOfWork
            .Repository<Order>()
            .Query()
            .AsNoTracking()
            .Where(order =>
                order.UserId == user.Id);

        query = ApplyOrderFilters(
            query,
            request);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        query = ApplyOrderSorting(
            query,
            request.SortBy,
            request.SortDirection);

        List<OrderSummaryResponse> items =
            await ProjectToOrderSummary(
                    query
                        .Skip(
                            (request.PageNumber - 1) *
                            request.PageSize)
                        .Take(request.PageSize))
                .ToListAsync(
                    cancellationToken);

        return CreatePagedResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    public async Task<OrderDetailsResponse>
    GetCurrentUserOrderByIdAsync(
        int orderId,
        CancellationToken cancellationToken =
            default)
    {
        ValidateOrderId(orderId);

        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        Order? order = await _unitOfWork
            .Repository<Order>()
            .Query()
            .AsNoTracking()
            .Include(currentOrder =>
                currentOrder.OrderItems)
            .FirstOrDefaultAsync(
                currentOrder =>
                    currentOrder.OrderId ==
                        orderId &&
                    currentOrder.UserId ==
                        user.Id,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "OrderNotFound");
        }

        return MapToOrderDetailsResponse(
            order);
    }

    public async Task<OrderDetailsResponse>
    CancelCurrentUserOrderAsync(
        int orderId,
        CancellationToken cancellationToken =
            default)
    {
        ValidateOrderId(orderId);

        ApplicationUser user =
            await GetCurrentUserAsync(
                cancellationToken);

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        try
        {
            Order? order = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Include(currentOrder =>
                    currentOrder.OrderItems)
                .FirstOrDefaultAsync(
                    currentOrder =>
                        currentOrder.OrderId ==
                            orderId &&
                        currentOrder.UserId ==
                            user.Id,
                    cancellationToken);

            if (order is null)
            {
                throw new KeyNotFoundException(
                    "OrderNotFound");
            }

            if (order.Status ==
                OrderStatus.Cancelled)
            {
                throw new ConflictException(
                    "OrderAlreadyCancelled");
            }

            if (order.Status ==
                OrderStatus.Delivered)
            {
                throw new ConflictException(
                    "DeliveredOrderCannotBeCancelled");
            }

            bool containsNonCancellableItem =
                order.OrderItems.Any(item =>
                    item.Status is
                        OrderItemStatus.Processing or
                        OrderItemStatus.Shipped or
                        OrderItemStatus.Delivered);

            if (containsNonCancellableItem)
            {
                throw new ConflictException(
                    "OrderCannotBeCancelled");
            }

            List<OrderItem> itemsToCancel =
                order.OrderItems
                    .Where(item =>
                        item.Status !=
                        OrderItemStatus.Cancelled)
                    .ToList();

            if (itemsToCancel.Count == 0)
            {
                throw new ConflictException(
                    "OrderAlreadyCancelled");
            }

            int[] listingIds = itemsToCancel
                .Select(item =>
                    item.ListingId)
                .Distinct()
                .ToArray();

            List<Listing> listings =
                await _unitOfWork
                    .Repository<Listing>()
                    .Query()
                    .IgnoreQueryFilters()
                    .Include(listing =>
                        listing.Book)
                    .Where(listing =>
                        listingIds.Contains(
                            listing.ListingId))
                    .ToListAsync(
                        cancellationToken);

            Dictionary<int, Listing> listingsById =
                listings.ToDictionary(
                    listing =>
                        listing.ListingId);

            DateTime now = DateTime.UtcNow;

            foreach (OrderItem orderItem in
                     itemsToCancel)
            {
                if (!listingsById.TryGetValue(
                        orderItem.ListingId,
                        out Listing? listing))
                {
                    throw new ConflictException(
                        "OrderListingNotFound");
                }

                listing.Quantity +=
                    orderItem.Quantity;

                if (listing.Status ==
                        ListingStatus.OutOfStock &&
                    listing.Quantity > 0 &&
                    !listing.IsDeleted &&
                    !listing.Book.IsDeleted)
                {
                    listing.Status =
                        ListingStatus.Active;
                }

                listing.UpdatedAt = now;
                listing.UpdatedById = user.Id;

                orderItem.Status =
                    OrderItemStatus.Cancelled;
            }

            order.Status =
                OrderStatus.Cancelled;

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return MapToOrderDetailsResponse(
                order);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw new ConflictException(
                "OrderCancellationConcurrencyConflict");
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }


    public async Task<
        PagedResponse<SellerOrderItemResponse>>
    GetCurrentSellerOrderItemsAsync(
        SellerOrderItemFilterRequest request,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser seller =
            await GetCurrentSellerAsync(
                cancellationToken);

        IQueryable<OrderItem> query = _unitOfWork
            .Repository<OrderItem>()
            .Query()
            .AsNoTracking()
            .Where(orderItem =>
                orderItem.SellerId == seller.Id);

        query = ApplySellerOrderItemFilters(
            query,
            request);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        query = ApplySellerOrderItemSorting(
            query,
            request.SortBy,
            request.SortDirection);

        List<SellerOrderItemResponse> items =
            await ProjectToSellerOrderItemResponse(
                    query
                        .Skip(
                            (request.PageNumber - 1) *
                            request.PageSize)
                        .Take(request.PageSize))
                .ToListAsync(
                    cancellationToken);

        return CreatePagedResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }


    public async Task<SellerOrderItemResponse>
    UpdateCurrentSellerOrderItemStatusAsync(
        int orderItemId,
        UpdateOrderItemStatusRequest request,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateOrderItemId(orderItemId);
        ValidateOrderItemStatus(request.Status);

        ApplicationUser seller =
            await GetCurrentSellerAsync(
                cancellationToken);

        OrderItem? orderItem = await _unitOfWork
            .Repository<OrderItem>()
            .Query()
            .Include(item =>
                item.Order)
            .ThenInclude(order =>
                order.OrderItems)
            .FirstOrDefaultAsync(
                item =>
                    item.OrderItemId ==
                        orderItemId &&
                    item.SellerId ==
                        seller.Id,
                cancellationToken);

        if (orderItem is null)
        {
            throw new KeyNotFoundException(
                "OrderItemNotFound");
        }

        if (orderItem.Status ==
            request.Status)
        {
            return MapToSellerOrderItemResponse(
                orderItem);
        }

        ValidateSellerStatusTransition(
            orderItem.Status,
            request.Status);

        orderItem.Status =
            request.Status;

        RecalculateOrderStatus(
            orderItem.Order);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return MapToSellerOrderItemResponse(
            orderItem);
    }


    public async Task<
        PagedResponse<OrderSummaryResponse>>
    GetAllOrdersAsync(
        OrderFilterRequest request,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(request);

        IQueryable<Order> query = _unitOfWork
            .Repository<Order>()
            .Query()
            .AsNoTracking();

        query = ApplyOrderFilters(
            query,
            request);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        query = ApplyOrderSorting(
            query,
            request.SortBy,
            request.SortDirection);

        List<OrderSummaryResponse> items =
            await ProjectToOrderSummary(
                    query
                        .Skip(
                            (request.PageNumber - 1) *
                            request.PageSize)
                        .Take(request.PageSize))
                .ToListAsync(
                    cancellationToken);

        return CreatePagedResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }


    public async Task<OrderDetailsResponse>
    GetOrderByIdForAdminAsync(
        int orderId,
        CancellationToken cancellationToken =
            default)
    {
        ValidateOrderId(orderId);

        Order? order = await _unitOfWork
            .Repository<Order>()
            .Query()
            .AsNoTracking()
            .Include(currentOrder =>
                currentOrder.OrderItems)
            .FirstOrDefaultAsync(
                currentOrder =>
                    currentOrder.OrderId ==
                        orderId,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "OrderNotFound");
        }

        return MapToOrderDetailsResponse(
            order);
    }


    public async Task<OrderDetailsResponse>
    UpdateOrderItemStatusForAdminAsync(
        int orderItemId,
        UpdateOrderItemStatusRequest request,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateOrderItemId(orderItemId);
        ValidateOrderItemStatus(request.Status);

        ApplicationUser admin =
            await GetCurrentUserAsync(
                cancellationToken);

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        try
        {
            OrderItem? orderItem = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .Include(item =>
                    item.Order)
                .ThenInclude(order =>
                    order.OrderItems)
                .FirstOrDefaultAsync(
                    item =>
                        item.OrderItemId ==
                        orderItemId,
                    cancellationToken);

            if (orderItem is null)
            {
                throw new KeyNotFoundException(
                    "OrderItemNotFound");
            }

            if (orderItem.Status ==
                request.Status)
            {
                await transaction.CommitAsync(
                    cancellationToken);

                return MapToOrderDetailsResponse(
                    orderItem.Order);
            }

            ValidateAdminStatusTransition(
                orderItem.Status,
                request.Status);

            if (request.Status ==
                OrderItemStatus.Cancelled)
            {
                Listing? listing = await _unitOfWork
                    .Repository<Listing>()
                    .Query()
                    .IgnoreQueryFilters()
                    .Include(currentListing =>
                        currentListing.Book)
                    .FirstOrDefaultAsync(
                        currentListing =>
                            currentListing.ListingId ==
                            orderItem.ListingId,
                        cancellationToken);

                if (listing is null)
                {
                    throw new ConflictException(
                        "OrderListingNotFound");
                }

                listing.Quantity +=
                    orderItem.Quantity;

                if (listing.Status ==
                        ListingStatus.OutOfStock &&
                    listing.Quantity > 0 &&
                    !listing.IsDeleted &&
                    !listing.Book.IsDeleted)
                {
                    listing.Status =
                        ListingStatus.Active;
                }

                listing.UpdatedAt =
                    DateTime.UtcNow;

                listing.UpdatedById =
                    admin.Id;
            }

            orderItem.Status =
                request.Status;

            RecalculateOrderStatus(
                orderItem.Order);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return MapToOrderDetailsResponse(
                orderItem.Order);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw new ConflictException(
                "OrderStatusConcurrencyConflict");
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
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
                        currentUser.Id ==
                        userId &&
                        !currentUser.IsDeleted,
                    cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "UserNotFound");
        }

        return user;
    }

    private async Task<ApplicationUser>
    GetCurrentSellerAsync(
        CancellationToken cancellationToken)
    {
        string userId =
            _currentUserService.GetUserId();

        ApplicationUser? seller =
            await _userManager
                .Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user =>
                        user.Id == userId &&
                        !user.IsDeleted,
                    cancellationToken);

        if (seller is null)
        {
            throw new KeyNotFoundException(
                "UserNotFound");
        }

        bool isSeller =
            await _userManager.IsInRoleAsync(
                seller,
                ApplicationRoles.Seller);

        if (!isSeller)
        {
            throw new ForbiddenException(
                "SellerRoleRequired");
        }

        if (string.IsNullOrWhiteSpace(
                seller.StoreName))
        {
            throw new InvalidOperationException(
                "SellerProfileNotActivated");
        }

        return seller;
    }


    private async Task<Coupon>
        GetValidCouponAsync(
            string couponCode,
            DateTime currentDate,
            CancellationToken cancellationToken)
    {
        Coupon? coupon = await _unitOfWork
            .Repository<Coupon>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                currentCoupon =>
                    currentCoupon.Code ==
                    couponCode,
                cancellationToken);

        if (coupon is null)
        {
            throw new KeyNotFoundException(
                "CouponNotFound");
        }

        if (!coupon.IsActive)
        {
            throw new InvalidOperationException(
                "CouponInactive");
        }

        if (currentDate < coupon.StartDate)
        {
            throw new InvalidOperationException(
                "CouponNotStarted");
        }

        if (currentDate > coupon.EndDate)
        {
            throw new InvalidOperationException(
                "CouponExpired");
        }

        if (coupon.DiscountValue <= 0m)
        {
            throw new InvalidOperationException(
                "InvalidCouponDiscount");
        }

        return coupon;
    }

    private static decimal
        CalculateCouponDiscount(
            Coupon coupon,
            decimal amountAfterListingDiscount)
    {
        string discountType =
            coupon.DiscountType.Trim();

        if (string.Equals(
                discountType,
                "Percentage",
                StringComparison.OrdinalIgnoreCase))
        {
            if (coupon.DiscountValue > 100m)
            {
                throw new InvalidOperationException(
                    "InvalidCouponDiscount");
            }

            return amountAfterListingDiscount *
                   coupon.DiscountValue /
                   100m;
        }

        if (string.Equals(
                discountType,
                "Fixed",
                StringComparison.OrdinalIgnoreCase))
        {
            return Math.Min(
                coupon.DiscountValue,
                amountAfterListingDiscount);
        }

        throw new InvalidOperationException(
            "UnsupportedCouponDiscountType");
    }

    private static void
        ValidateListingForCheckout(
            Listing listing,
            int requestedQuantity)
    {
        if (requestedQuantity <= 0)
        {
            throw new InvalidOperationException(
                "InvalidCartItemQuantity");
        }

        if (listing.IsDeleted)
        {
            throw new InvalidOperationException(
                "ListingNotAvailable");
        }

        if (listing.Book.IsDeleted)
        {
            throw new InvalidOperationException(
                "ListingBookNotAvailable");
        }

        if (listing.Status ==
                ListingStatus.OutOfStock ||
            listing.Quantity <= 0)
        {
            throw new InvalidOperationException(
                "ListingOutOfStock");
        }

        if (listing.Status !=
            ListingStatus.Active)
        {
            throw new InvalidOperationException(
                "ListingNotAvailable");
        }

        if (requestedQuantity >
            listing.Quantity)
        {
            throw new InvalidOperationException(
                "InsufficientStock");
        }

        if (listing.Price <= 0m)
        {
            throw new InvalidOperationException(
                "InvalidListingPrice");
        }

        if (listing.DiscountPercentage < 0m ||
            listing.DiscountPercentage > 100m)
        {
            throw new InvalidOperationException(
                "InvalidListingDiscount");
        }
    }

    private static IQueryable<Order>
    ApplyOrderFilters(
        IQueryable<Order> query,
        OrderFilterRequest request)
    {
        if (request.Status.HasValue)
        {
            query = query.Where(order =>
                order.Status ==
                    request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(order =>
                order.OrderDate >=
                    request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(order =>
                order.OrderDate <=
                    request.ToDate.Value);
        }

        return query;
    }

    private static IQueryable<OrderItem>
    ApplySellerOrderItemFilters(
        IQueryable<OrderItem> query,
        SellerOrderItemFilterRequest request)
    {
        if (request.OrderId.HasValue)
        {
            query = query.Where(orderItem =>
                orderItem.OrderId ==
                    request.OrderId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(orderItem =>
                orderItem.Status ==
                    request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(orderItem =>
                orderItem.Order.OrderDate >=
                    request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(orderItem =>
                orderItem.Order.OrderDate <=
                    request.ToDate.Value);
        }

        return query;
    }

    private static IQueryable<OrderItem>
    ApplySellerOrderItemSorting(
        IQueryable<OrderItem> query,
        string sortBy,
        string sortDirection)
    {
        string normalizedSortBy =
            sortBy.Trim().ToLowerInvariant();

        bool descending = string.Equals(
            sortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "linetotal" or "total" =>
                descending
                    ? query
                        .OrderByDescending(orderItem =>
                            orderItem.LineTotal)
                        .ThenByDescending(orderItem =>
                            orderItem.OrderItemId)
                    : query
                        .OrderBy(orderItem =>
                            orderItem.LineTotal)
                        .ThenBy(orderItem =>
                            orderItem.OrderItemId),

            "status" =>
                descending
                    ? query
                        .OrderByDescending(orderItem =>
                            orderItem.Status)
                        .ThenByDescending(orderItem =>
                            orderItem.OrderItemId)
                    : query
                        .OrderBy(orderItem =>
                            orderItem.Status)
                        .ThenBy(orderItem =>
                            orderItem.OrderItemId),

            _ =>
                descending
                    ? query
                        .OrderByDescending(orderItem =>
                            orderItem.Order.OrderDate)
                        .ThenByDescending(orderItem =>
                            orderItem.OrderItemId)
                    : query
                        .OrderBy(orderItem =>
                            orderItem.Order.OrderDate)
                        .ThenBy(orderItem =>
                            orderItem.OrderItemId)
        };
    }

    private static IQueryable<
        SellerOrderItemResponse>
    ProjectToSellerOrderItemResponse(
        IQueryable<OrderItem> query)
    {
        return query.Select(orderItem =>
            new SellerOrderItemResponse
            {
                OrderItemId =
                    orderItem.OrderItemId,
                OrderId =
                    orderItem.OrderId,
                OrderDate =
                    orderItem.Order.OrderDate,
                ListingId =
                    orderItem.ListingId,
                BookId =
                    orderItem.BookId,
                SellerId =
                    orderItem.SellerId,
                BookTitle =
                    orderItem.BookTitleSnapshot,
                SellerStoreName =
                    orderItem
                        .SellerStoreNameSnapshot,
                Format =
                    orderItem.FormatSnapshot,
                Condition =
                    orderItem.ConditionSnapshot,
                Quantity =
                    orderItem.Quantity,
                UnitPrice =
                    orderItem.UnitPrice,
                DiscountPercentage =
                    orderItem.DiscountPercentage,
                EffectiveUnitPrice =
                    orderItem.EffectiveUnitPrice,
                LineSubtotal =
                    orderItem.LineSubtotal,
                LineDiscount =
                    orderItem.LineDiscount,
                LineTotal =
                    orderItem.LineTotal,
                Status =
                    orderItem.Status
            });
    }


    private static IQueryable<Order>
        ApplyOrderSorting(
            IQueryable<Order> query,
            string sortBy,
            string sortDirection)
    {
        string normalizedSortBy =
            sortBy.Trim().ToLowerInvariant();

        bool descending = string.Equals(
            sortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "totalamount" or "total" =>
                descending
                    ? query.OrderByDescending(order =>
                        order.TotalAmount)
                        .ThenByDescending(order =>
                            order.OrderId)
                    : query.OrderBy(order =>
                        order.TotalAmount)
                        .ThenBy(order =>
                            order.OrderId),

            "status" =>
                descending
                    ? query.OrderByDescending(order =>
                        order.Status)
                        .ThenByDescending(order =>
                            order.OrderId)
                    : query.OrderBy(order =>
                        order.Status)
                        .ThenBy(order =>
                            order.OrderId),

            _ =>
                descending
                    ? query.OrderByDescending(order =>
                        order.OrderDate)
                        .ThenByDescending(order =>
                            order.OrderId)
                    : query.OrderBy(order =>
                        order.OrderDate)
                        .ThenBy(order =>
                            order.OrderId)
        };
    }

    private static IQueryable<OrderSummaryResponse>
    ProjectToOrderSummary(
        IQueryable<Order> query)
    {
        return query.Select(order =>
            new OrderSummaryResponse
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalItems =
                    order.OrderItems.Sum(
                        item => item.Quantity),
                SubtotalAmount =
                    order.SubtotalAmount,
                ListingDiscountAmount =
                    order.ListingDiscountAmount,
                CouponDiscountAmount =
                    order.CouponDiscountAmount,
                TotalDiscountAmount =
                    order.TotalDiscountAmount,
                TotalAmount =
                    order.TotalAmount,
                CouponCode =
                    order.CouponCodeSnapshot
            });
    }

    private static PagedResponse<T>
    CreatePagedResponse<T>(
        IReadOnlyCollection<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        return new PagedResponse<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize)
        };
    }

    private static void ValidateOrderItemId(
    int orderItemId)
    {
        if (orderItemId <= 0)
        {
            throw new InvalidOperationException(
                "OrderItemIdMustBeGreaterThanZero");
        }
    }

    private static void ValidateOrderItemStatus(
    OrderItemStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new InvalidOperationException(
                "InvalidOrderItemStatus");
        }
    }

    private static void
    ValidateAdminStatusTransition(
        OrderItemStatus currentStatus,
        OrderItemStatus requestedStatus)
    {
        bool isAllowed =
            currentStatus switch
            {
                OrderItemStatus.Pending =>
                    requestedStatus is
                        OrderItemStatus.Confirmed or
                        OrderItemStatus.Cancelled,

                OrderItemStatus.Confirmed =>
                    requestedStatus is
                        OrderItemStatus.Processing or
                        OrderItemStatus.Cancelled,

                OrderItemStatus.Processing =>
                    requestedStatus ==
                    OrderItemStatus.Shipped,

                OrderItemStatus.Shipped =>
                    requestedStatus ==
                    OrderItemStatus.Delivered,

                _ => false
            };

        if (!isAllowed)
        {
            throw new ConflictException(
                "InvalidOrderItemStatusTransition");
        }
    }


    private static void
    ValidateSellerStatusTransition(
        OrderItemStatus currentStatus,
        OrderItemStatus requestedStatus)
    {
        bool isAllowed =
            currentStatus switch
            {
                OrderItemStatus.Pending =>
                    requestedStatus ==
                    OrderItemStatus.Confirmed,

                OrderItemStatus.Confirmed =>
                    requestedStatus ==
                    OrderItemStatus.Processing,

                OrderItemStatus.Processing =>
                    requestedStatus ==
                    OrderItemStatus.Shipped,

                OrderItemStatus.Shipped =>
                    requestedStatus ==
                    OrderItemStatus.Delivered,

                _ => false
            };

        if (!isAllowed)
        {
            throw new ConflictException(
                "InvalidOrderItemStatusTransition");
        }
    }

    private static void RecalculateOrderStatus(
    Order order)
    {
        List<OrderItem> items =
            order.OrderItems.ToList();

        if (items.Count == 0)
        {
            order.Status =
                OrderStatus.Pending;

            return;
        }

        if (items.All(item =>
                item.Status ==
                OrderItemStatus.Cancelled))
        {
            order.Status =
                OrderStatus.Cancelled;

            return;
        }

        List<OrderItem> activeItems =
            items
                .Where(item =>
                    item.Status !=
                    OrderItemStatus.Cancelled)
                .ToList();

        if (activeItems.All(item =>
                item.Status ==
                OrderItemStatus.Delivered))
        {
            order.Status =
                OrderStatus.Delivered;

            return;
        }

        if (activeItems.Any(item =>
                item.Status is
                    OrderItemStatus.Shipped or
                    OrderItemStatus.Delivered))
        {
            order.Status =
                OrderStatus.Shipped;

            return;
        }

        if (activeItems.Any(item =>
                item.Status ==
                OrderItemStatus.Processing))
        {
            order.Status =
                OrderStatus.Processing;

            return;
        }

        if (activeItems.Any(item =>
                item.Status ==
                OrderItemStatus.Confirmed))
        {
            order.Status =
                OrderStatus.Confirmed;

            return;
        }

        order.Status =
            OrderStatus.Pending;
    }

    private static SellerOrderItemResponse
    MapToSellerOrderItemResponse(
        OrderItem orderItem)
    {
        return new SellerOrderItemResponse
        {
            OrderItemId =
                orderItem.OrderItemId,
            OrderId =
                orderItem.OrderId,
            OrderDate =
                orderItem.Order.OrderDate,
            ListingId =
                orderItem.ListingId,
            BookId =
                orderItem.BookId,
            SellerId =
                orderItem.SellerId,
            BookTitle =
                orderItem.BookTitleSnapshot,
            SellerStoreName =
                orderItem
                    .SellerStoreNameSnapshot,
            Format =
                orderItem.FormatSnapshot,
            Condition =
                orderItem.ConditionSnapshot,
            Quantity =
                orderItem.Quantity,
            UnitPrice =
                orderItem.UnitPrice,
            DiscountPercentage =
                orderItem.DiscountPercentage,
            EffectiveUnitPrice =
                orderItem.EffectiveUnitPrice,
            LineSubtotal =
                orderItem.LineSubtotal,
            LineDiscount =
                orderItem.LineDiscount,
            LineTotal =
                orderItem.LineTotal,
            Status =
                orderItem.Status
        };
    }


    private static void ValidateOrderId(
    int orderId)
    {
        if (orderId <= 0)
        {
            throw new InvalidOperationException(
                "OrderIdMustBeGreaterThanZero");
        }
    }


    private static string? NormalizeCouponCode(
        string? couponCode)
    {
        return string.IsNullOrWhiteSpace(
            couponCode)
            ? null
            : couponCode.Trim();
    }

    private static OrderDetailsResponse
        MapToOrderDetailsResponse(
            Order order)
    {
        return new OrderDetailsResponse
        {
            OrderId = order.OrderId,
            OrderDate = order.OrderDate,
            UserId = order.UserId,
            Status = order.Status,
            TotalItems = order.OrderItems.Sum(
                item => item.Quantity),
            SubtotalAmount =
                order.SubtotalAmount,
            ListingDiscountAmount =
                order.ListingDiscountAmount,
            CouponDiscountAmount =
                order.CouponDiscountAmount,
            TotalDiscountAmount =
                order.TotalDiscountAmount,
            TotalAmount =
                order.TotalAmount,
            CouponCode =
                order.CouponCodeSnapshot,
            CouponDiscountType =
                order
                    .CouponDiscountTypeSnapshot,
            CouponDiscountValue =
                order
                    .CouponDiscountValueSnapshot,
            Items = order.OrderItems
                .OrderBy(item =>
                    item.OrderItemId)
                .Select(MapToOrderItemResponse)
                .ToList()
        };
    }

    private static OrderItemResponse
        MapToOrderItemResponse(
            OrderItem orderItem)
    {
        return new OrderItemResponse
        {
            OrderItemId =
                orderItem.OrderItemId,
            ListingId =
                orderItem.ListingId,
            BookId =
                orderItem.BookId,
            SellerId =
                orderItem.SellerId,
            BookTitle =
                orderItem.BookTitleSnapshot,
            SellerStoreName =
                orderItem
                    .SellerStoreNameSnapshot,
            Format =
                orderItem.FormatSnapshot,
            Condition =
                orderItem.ConditionSnapshot,
            Quantity =
                orderItem.Quantity,
            UnitPrice =
                orderItem.UnitPrice,
            DiscountPercentage =
                orderItem.DiscountPercentage,
            EffectiveUnitPrice =
                orderItem.EffectiveUnitPrice,
            LineSubtotal =
                orderItem.LineSubtotal,
            LineDiscount =
                orderItem.LineDiscount,
            LineTotal =
                orderItem.LineTotal,
            Status =
                orderItem.Status
        };
    }
}