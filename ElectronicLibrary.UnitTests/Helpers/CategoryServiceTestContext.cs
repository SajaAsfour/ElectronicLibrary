using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Mapping;
using ElectronicLibrary.BLL.Services.Catalog;
using ElectronicLibrary.DAL.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class CategoryServiceTestContext
    : IAsyncDisposable
{
    private static readonly Lazy<bool> MappingRegistration =
        new(() =>
        {
            TypeAdapterConfig.GlobalSettings.Scan(
                typeof(MapsterConfig).Assembly);

            return true;
        });

    public CategoryServiceTestContext()
    {
        _ = MappingRegistration.Value;

        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"CategoryTests-{Guid.NewGuid()}")
                .Options;

        DbContext = new ApplicationDbContext(options);

        CurrentUserService =
            new FakeCurrentUserService();

        UnitOfWork =
            new TestUnitOfWork(DbContext);

        CategoryService = new CategoryService(
            UnitOfWork,
            CurrentUserService);
    }

    public ApplicationDbContext DbContext { get; }

    public FakeCurrentUserService CurrentUserService { get; }

    public TestUnitOfWork UnitOfWork { get; }

    public ICategoryService CategoryService { get; }

    public async ValueTask DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.DisposeAsync();
    }
}