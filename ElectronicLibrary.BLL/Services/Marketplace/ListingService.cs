using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.BLL.Interfaces.Marketplace;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.DTOs.Requests.Listings;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Listings;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.BLL.Services.Marketplace;

public class ListingService : IListingService
{
    private const int MaximumPageSize = 50;

    private const decimal MaximumPrice =
        9999999999999999.99m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ListingService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<ListingResponse> CreateListingAsync(
        CreateListingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser seller =
            await GetCurrentSellerAsync(
                cancellationToken);

        ValidateListingValues(
            request.Price,
            request.Quantity,
            request.Format,
            request.Condition,
            request.DiscountPercentage);

        bool bookExists = await _unitOfWork
            .Repository<Book>()
            .Query()
            .AsNoTracking()
            .AnyAsync(
                book =>
                    book.BookId == request.BookId,
                cancellationToken);

        if (!bookExists)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        DateTime now = DateTime.UtcNow;

        var listing = new Listing
        {
            BookId = request.BookId,
            SellerId = seller.Id,
            Price = request.Price,
            Quantity = request.Quantity,
            Format = request.Format,
            Condition = request.Condition,
            DiscountPercentage =
                request.DiscountPercentage,
            Status = ListingStatus.Draft,
            CreatedAt = now,
            CreatedById = seller.Id
        };

        await _unitOfWork
            .Repository<Listing>()
            .AddAsync(
                listing,
                cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetSellerListingResponseAsync(
            listing.ListingId,
            seller.Id,
            cancellationToken);
    }

    public async Task<ListingResponse>
        GetListingByIdAsync(
            int listingId,
            CancellationToken cancellationToken = default)
    {
        ValidateListingId(listingId);

        IQueryable<Listing> query = _unitOfWork
            .Repository<Listing>()
            .Query()
            .AsNoTracking()
            .Where(listing =>
                listing.ListingId == listingId &&
                listing.Status ==
                    ListingStatus.Active &&
                listing.Quantity > 0);

        ListingResponse? response =
            await ProjectToResponse(query)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (response is null)
        {
            throw new KeyNotFoundException(
                "ListingNotFound");
        }

        return response;
    }

    public async Task<ListingResponse>
        UpdateListingAsync(
            int listingId,
            UpdateListingRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser seller =
            await GetCurrentSellerAsync(
                cancellationToken);

        Listing listing =
            await GetTrackedOwnedListingAsync(
                listingId,
                seller.Id,
                cancellationToken);

        ValidateListingValues(
            request.Price,
            request.Quantity,
            request.Format,
            request.Condition,
            request.DiscountPercentage);

        listing.Price = request.Price;
        listing.Quantity = request.Quantity;
        listing.Format = request.Format;
        listing.Condition = request.Condition;
        listing.DiscountPercentage =
            request.DiscountPercentage;

        if (listing.Status ==
                ListingStatus.Active &&
            listing.Quantity == 0)
        {
            listing.Status =
                ListingStatus.OutOfStock;
        }

        listing.UpdatedAt = DateTime.UtcNow;
        listing.UpdatedById = seller.Id;

        _unitOfWork
            .Repository<Listing>()
            .Update(listing);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetSellerListingResponseAsync(
            listing.ListingId,
            seller.Id,
            cancellationToken);
    }

    public async Task<ListingResponse>
        UpdateListingStatusAsync(
            int listingId,
            UpdateListingStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser seller =
            await GetCurrentSellerAsync(
                cancellationToken);

        Listing listing =
            await GetTrackedOwnedListingAsync(
                listingId,
                seller.Id,
                cancellationToken);

        ValidateListingStatus(request.Status);

        if (request.Status ==
            ListingStatus.Suspended)
        {
            throw new ForbiddenException(
                "SellerCannotSuspendListing");
        }

        if (listing.Status ==
            ListingStatus.Suspended)
        {
            throw new ForbiddenException(
                "SuspendedListingManagedByAdmin");
        }

        if (request.Status ==
            ListingStatus.Active)
        {
            ValidateListingValues(
                listing.Price,
                listing.Quantity,
                listing.Format,
                listing.Condition,
                listing.DiscountPercentage);

            if (listing.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "ActiveListingRequiresStock");
            }
        }

        if (listing.Status == request.Status)
        {
            return await GetSellerListingResponseAsync(
                listing.ListingId,
                seller.Id,
                cancellationToken);
        }

        listing.Status = request.Status;
        listing.UpdatedAt = DateTime.UtcNow;
        listing.UpdatedById = seller.Id;

        _unitOfWork
            .Repository<Listing>()
            .Update(listing);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetSellerListingResponseAsync(
            listing.ListingId,
            seller.Id,
            cancellationToken);
    }

    public async Task DeleteListingAsync(
        int listingId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser seller =
            await GetCurrentSellerAsync(
                cancellationToken);

        Listing listing =
            await GetTrackedOwnedListingAsync(
                listingId,
                seller.Id,
                cancellationToken);

        listing.IsDeleted = true;
        listing.DeletedAt = DateTime.UtcNow;
        listing.DeletedById = seller.Id;

        _unitOfWork
            .Repository<Listing>()
            .Update(listing);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<PagedResponse<ListingResponse>>
        GetCurrentSellerListingsAsync(
            SellerListingFilterRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidatePagination(
            request.PageNumber,
            request.PageSize);

        if (request.Status.HasValue)
        {
            ValidateListingStatus(
                request.Status.Value);
        }

        ApplicationUser seller =
            await GetCurrentSellerAsync(
                cancellationToken);

        IQueryable<Listing> query = _unitOfWork
            .Repository<Listing>()
            .Query()
            .AsNoTracking()
            .Where(listing =>
                listing.SellerId == seller.Id);

        if (request.Status.HasValue)
        {
            query = query.Where(listing =>
                listing.Status ==
                    request.Status.Value);
        }

        int totalCount = await query.CountAsync(
            cancellationToken);

        IQueryable<Listing> pagedQuery = query
            .OrderByDescending(listing =>
                listing.CreatedAt)
            .ThenByDescending(listing =>
                listing.ListingId)
            .Skip(
                (request.PageNumber - 1) *
                request.PageSize)
            .Take(request.PageSize);

        List<ListingResponse> items =
            await ProjectToResponse(pagedQuery)
                .ToListAsync(cancellationToken);

        return CreatePagedResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    public async Task<PagedResponse<ListingResponse>>
        GetBookListingsAsync(
            int bookId,
            BookListingFilterRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (bookId <= 0)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        ValidatePagination(
            request.PageNumber,
            request.PageSize);

        if (request.Format.HasValue &&
            !Enum.IsDefined(
                typeof(BookFormat),
                request.Format.Value))
        {
            throw new InvalidOperationException(
                "InvalidBookFormat");
        }

        if (request.Condition.HasValue &&
            !Enum.IsDefined(
                typeof(BookCondition),
                request.Condition.Value))
        {
            throw new InvalidOperationException(
                "InvalidBookCondition");
        }

        bool bookExists = await _unitOfWork
            .Repository<Book>()
            .Query()
            .AsNoTracking()
            .AnyAsync(
                book =>
                    book.BookId == bookId,
                cancellationToken);

        if (!bookExists)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        IQueryable<Listing> query = _unitOfWork
            .Repository<Listing>()
            .Query()
            .AsNoTracking()
            .Where(listing =>
                listing.BookId == bookId &&
                listing.Status ==
                    ListingStatus.Active &&
                listing.Quantity > 0);

        if (request.Format.HasValue)
        {
            query = query.Where(listing =>
                listing.Format ==
                    request.Format.Value);
        }

        if (request.Condition.HasValue)
        {
            query = query.Where(listing =>
                listing.Condition ==
                    request.Condition.Value);
        }

        int totalCount = await query.CountAsync(
            cancellationToken);

        IQueryable<Listing> pagedQuery = query
            .OrderBy(listing =>
                listing.Price -
                listing.Price *
                listing.DiscountPercentage /
                100m)
            .ThenBy(listing =>
                listing.ListingId)
            .Skip(
                (request.PageNumber - 1) *
                request.PageSize)
            .Take(request.PageSize);

        List<ListingResponse> items =
            await ProjectToResponse(pagedQuery)
                .ToListAsync(cancellationToken);

        return CreatePagedResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
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

    private async Task<Listing>
        GetTrackedOwnedListingAsync(
            int listingId,
            string sellerId,
            CancellationToken cancellationToken)
    {
        ValidateListingId(listingId);

        Listing? listing = await _unitOfWork
            .Repository<Listing>()
            .Query()
            .FirstOrDefaultAsync(
                currentListing =>
                    currentListing.ListingId ==
                    listingId,
                cancellationToken);

        if (listing is null)
        {
            throw new KeyNotFoundException(
                "ListingNotFound");
        }

        if (!string.Equals(
                listing.SellerId,
                sellerId,
                StringComparison.Ordinal))
        {
            throw new ForbiddenException(
                "ListingOwnershipRequired");
        }

        return listing;
    }

    private async Task<ListingResponse>
        GetSellerListingResponseAsync(
            int listingId,
            string sellerId,
            CancellationToken cancellationToken)
    {
        IQueryable<Listing> query = _unitOfWork
            .Repository<Listing>()
            .Query()
            .AsNoTracking()
            .Where(listing =>
                listing.ListingId == listingId &&
                listing.SellerId == sellerId);

        ListingResponse? response =
            await ProjectToResponse(query)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (response is null)
        {
            throw new KeyNotFoundException(
                "ListingNotFound");
        }

        return response;
    }

    private static IQueryable<ListingResponse>
        ProjectToResponse(
            IQueryable<Listing> query)
    {
        return query.Select(listing =>
            new ListingResponse
            {
                ListingId = listing.ListingId,
                BookId = listing.BookId,
                BookTitle = listing.Book.Title,
                SellerId = listing.SellerId,
                StoreName =
                    listing.Seller.StoreName ??
                    string.Empty,
                Price = listing.Price,
                DiscountPercentage =
                    listing.DiscountPercentage,
                EffectivePrice =
                    listing.Price -
                    listing.Price *
                    listing.DiscountPercentage /
                    100m,
                Quantity = listing.Quantity,
                Format = listing.Format,
                Condition = listing.Condition,
                Status = listing.Status,
                IsAvailable =
                    listing.Status ==
                        ListingStatus.Active &&
                    listing.Quantity > 0,
                CreatedAt = listing.CreatedAt,
                UpdatedAt = listing.UpdatedAt
            });
    }

    private static PagedResponse<ListingResponse>
        CreatePagedResponse(
            IReadOnlyCollection<ListingResponse> items,
            int pageNumber,
            int pageSize,
            int totalCount)
    {
        return new PagedResponse<ListingResponse>
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

    private static void ValidateListingValues(
        decimal price,
        int quantity,
        BookFormat format,
        BookCondition? condition,
        decimal discountPercentage)
    {
        if (price <= 0 ||
            price > MaximumPrice)
        {
            throw new InvalidOperationException(
                "ListingPriceOutOfRange");
        }

        if (quantity < 0)
        {
            throw new InvalidOperationException(
                "ListingQuantityCannotBeNegative");
        }

        if (!Enum.IsDefined(
                typeof(BookFormat),
                format))
        {
            throw new InvalidOperationException(
                "InvalidBookFormat");
        }

        if (condition.HasValue &&
            !Enum.IsDefined(
                typeof(BookCondition),
                condition.Value))
        {
            throw new InvalidOperationException(
                "InvalidBookCondition");
        }

        if (discountPercentage < 0 ||
            discountPercentage > 100)
        {
            throw new InvalidOperationException(
                "DiscountPercentageOutOfRange");
        }

        if (format == BookFormat.Physical &&
            !condition.HasValue)
        {
            throw new InvalidOperationException(
                "PhysicalListingConditionRequired");
        }

        if (format != BookFormat.Physical &&
            condition.HasValue)
        {
            throw new InvalidOperationException(
                "NonPhysicalListingConditionNotAllowed");
        }
    }

    private static void ValidateListingStatus(
        ListingStatus status)
    {
        if (!Enum.IsDefined(
                typeof(ListingStatus),
                status))
        {
            throw new InvalidOperationException(
                "InvalidListingStatus");
        }
    }

    private static void ValidateListingId(
        int listingId)
    {
        if (listingId <= 0)
        {
            throw new KeyNotFoundException(
                "ListingNotFound");
        }
    }

    private static void ValidatePagination(
        int pageNumber,
        int pageSize)
    {
        if (pageNumber <= 0)
        {
            throw new InvalidOperationException(
                "PageNumberMustBeGreaterThanZero");
        }

        if (pageSize <= 0 ||
            pageSize > MaximumPageSize)
        {
            throw new InvalidOperationException(
                "PageSizeOutOfRange");
        }
    }
}