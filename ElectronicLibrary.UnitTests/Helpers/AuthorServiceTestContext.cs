using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Mapping;
using ElectronicLibrary.BLL.Services.Catalog;
using ElectronicLibrary.DAL.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class AuthorServiceTestContext
    : IAsyncDisposable
{
    private static readonly Lazy<bool>
        MappingRegistration = new(() =>
        {
            TypeAdapterConfig.GlobalSettings.Scan(
                typeof(MapsterConfig).Assembly);

            return true;
        });

    public AuthorServiceTestContext()
    {
        _ = MappingRegistration.Value;

        var options =
            new DbContextOptionsBuilder<
                    ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"AuthorTests-{Guid.NewGuid()}")
                .Options;

        DbContext = new ApplicationDbContext(options);

        CurrentUserService =
            new FakeCurrentUserService();

        UnitOfWork =
            new TestUnitOfWork(DbContext);

        AuthorService = new AuthorService(
            UnitOfWork,
            CurrentUserService);
    }

    public ApplicationDbContext DbContext { get; }

    public FakeCurrentUserService
        CurrentUserService
    { get; }

    public TestUnitOfWork UnitOfWork { get; }

    public IAuthorService AuthorService { get; }

    public async ValueTask DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.DisposeAsync();
    }
}