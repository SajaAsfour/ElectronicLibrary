using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.BLL.Interfaces.UserManagement;
using ElectronicLibrary.BLL.Mapping;
using ElectronicLibrary.BLL.Services.Authentication;
using ElectronicLibrary.BLL.Services.Catalog;
using ElectronicLibrary.PL.Services.Common;
using ElectronicLibrary.BLL.Interfaces.Sellers;
using ElectronicLibrary.BLL.Services.Sellers;
using ElectronicLibrary.BLL.Services.UserManagement;
using ElectronicLibrary.DAL.Repositories.Generic;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;
using ElectronicLibrary.BLL.Interfaces.Marketplace;
using ElectronicLibrary.BLL.Services.Marketplace;
using Mapster;

namespace ElectronicLibrary.PL.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MapsterConfig).Assembly);

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IAuthorService, AuthorService>();

        services.AddScoped<IPublisherService, PublisherService>();

        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IBookService, BookService>();

        services.AddScoped<ISellerService, SellerService>();

        services.AddScoped<IListingService, ListingService>();

        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddScoped<IUserManagementService, UserManagementService>();

        services.AddScoped(typeof(IGenericRepository<>),typeof(GenericRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}