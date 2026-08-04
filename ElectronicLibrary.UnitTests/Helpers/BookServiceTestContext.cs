using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Models.Storage;
using ElectronicLibrary.BLL.Services.Catalog;
using ElectronicLibrary.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class BookServiceTestContext
    : IAsyncDisposable
{
    private readonly List<Stream> _openedStreams = [];

    public BookServiceTestContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"BookTests-{Guid.NewGuid()}")
                .Options;

        DbContext =
            new ApplicationDbContext(options);

        CurrentUserService =
            new FakeCurrentUserService();

        FileStorageService =
            new FakeFileStorageService();

        UnitOfWork =
            new TestUnitOfWork(DbContext);

        BookService =
            new BookService(
                UnitOfWork,
                CurrentUserService,
                FileStorageService);
    }

    public ApplicationDbContext DbContext { get; }

    public FakeCurrentUserService CurrentUserService { get; }

    public FakeFileStorageService FileStorageService { get; }

    public TestUnitOfWork UnitOfWork { get; }

    public IBookService BookService { get; }

    public FileUploadData CreateImageFile(
        string fileName = "book-cover.jpg",
        string contentType = "image/jpeg",
        int sizeInBytes = 128)
    {
        var stream =
            new MemoryStream(
                Enumerable
                    .Repeat((byte)1, sizeInBytes)
                    .ToArray());

        _openedStreams.Add(stream);

        return new FileUploadData
        {
            Content = stream,
            FileName = fileName,
            ContentType = contentType,
            Length = stream.Length
        };
    }

    public async ValueTask DisposeAsync()
    {
        foreach (Stream stream in _openedStreams)
        {
            await stream.DisposeAsync();
        }

        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.DisposeAsync();
    }
}