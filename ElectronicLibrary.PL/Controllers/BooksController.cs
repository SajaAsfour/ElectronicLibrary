using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Models.Storage;
using ElectronicLibrary.DAL.DTOs.Requests.Books;
using ElectronicLibrary.DAL.DTOs.Responses.Books;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.PL.Authorization;
using ElectronicLibrary.PL.Models.Requests.Books;
using ElectronicLibrary.PL.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public BooksController(
        IBookService bookService,
        IStringLocalizer<SharedResources> localizer)
    {
        _bookService = bookService;
        _localizer = localizer;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
    typeof(PagedResponse<BookResponse>),
    StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<BookResponse>>>
    GetBooks(
        [FromQuery] BookFilterRequest request,
        CancellationToken cancellationToken)
    {
        PagedResponse<BookResponse> response =
            await _bookService.GetBooksAsync(
                request,
                cancellationToken);

        foreach (BookResponse book in response.Items)
        {
            if (!string.IsNullOrWhiteSpace(
                    book.MainImageUrl))
            {
                book.MainImageUrl =
                    ToAbsoluteUrl(
                        book.MainImageUrl);
            }
        }

        return Ok(response);
    }

    [HttpGet("{bookId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(BookDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDetailsResponse>>
        GetBookById(
            int bookId,
            CancellationToken cancellationToken)
    {
        BookDetailsResponse response =
            await _bookService.GetBookByIdAsync(
                bookId,
                cancellationToken);

        return Ok(
            MakeImageUrlsAbsolute(response));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
    typeof(BookDetailsResponse),
    StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookDetailsResponse>>
    CreateBook(
        [FromForm] CreateBookRequest request,
        [FromForm] List<IFormFile>? images,
        [FromForm] int? mainImageIndex,
        CancellationToken cancellationToken)
    {
        List<Stream> openedStreams = [];

        try
        {
            IReadOnlyCollection<FileUploadData> imageFiles =
                OpenUploadFiles(
                    images,
                    openedStreams);

            BookDetailsResponse response =
                await _bookService.CreateBookAsync(
                    request,
                    imageFiles,
                    mainImageIndex,
                    cancellationToken);

            MakeImageUrlsAbsolute(response);

            return CreatedAtAction(
                nameof(GetBookById),
                new { bookId = response.BookId },
                response);
        }
        finally
        {
            await DisposeStreamsAsync(
                openedStreams);
        }
    }

    [HttpPut("{bookId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
    typeof(BookDetailsResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookDetailsResponse>>
    UpdateBook(
        int bookId,
        [FromForm] UpdateBookRequest request,
        [FromForm] List<IFormFile>? images,
        [FromForm] int? mainImageIndex,
        CancellationToken cancellationToken)
    {
        List<Stream> openedStreams = [];

        try
        {
            IReadOnlyCollection<FileUploadData> imageFiles =
                OpenUploadFiles(
                    images,
                    openedStreams);

            BookDetailsResponse response =
                await _bookService.UpdateBookAsync(
                    bookId,
                    request,
                    imageFiles,
                    mainImageIndex,
                    cancellationToken);

            return Ok(
                MakeImageUrlsAbsolute(response));
        }
        finally
        {
            await DisposeStreamsAsync(
                openedStreams);
        }
    }


    [HttpDelete("{bookId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>>
        DeleteBook(
            int bookId,
            CancellationToken cancellationToken)
    {
        await _bookService.DeleteBookAsync(
            bookId,
            cancellationToken);

        return Ok(new MessageResponse
        {
            Message = _localizer[
                "BookDeletedSuccessfully"].Value
        });
    }
    private static IReadOnlyCollection<FileUploadData>
    OpenUploadFiles(
        IEnumerable<IFormFile>? formFiles,
        ICollection<Stream> openedStreams)
    {
        if (formFiles is null)
        {
            return Array.Empty<FileUploadData>();
        }

        List<FileUploadData> files = [];

        foreach (IFormFile formFile in formFiles)
        {
            Stream stream =
                formFile.OpenReadStream();

            openedStreams.Add(stream);

            files.Add(
                new FileUploadData
                {
                    Content = stream,
                    FileName = formFile.FileName,
                    ContentType = formFile.ContentType,
                    Length = formFile.Length
                });
        }

        return files;
    }

    private static async Task DisposeStreamsAsync(
        IEnumerable<Stream> streams)
    {
        foreach (Stream stream in streams)
        {
            await stream.DisposeAsync();
        }
    }
    [HttpPost("{bookId:int}/images")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
    typeof(UploadBookImagesResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadBookImagesResponse>>
    UploadBookImages(
        int bookId,
        [FromForm] UploadBookImagesRequest request,
        CancellationToken cancellationToken)
    {
        List<Stream> openedStreams = [];

        try
        {
            IReadOnlyCollection<FileUploadData> files =
                OpenUploadFiles(
                    request.Images,
                    openedStreams);

            UploadBookImagesResponse response =
                await _bookService.UploadBookImagesAsync(
                    bookId,
                    files,
                    request.MainImageIndex,
                    cancellationToken);

            return Ok(
                MakeImageUrlsAbsolute(response));
        }
        finally
        {
            await DisposeStreamsAsync(openedStreams);
        }
    }
    [HttpPut("{bookId:int}/images/{bookImageId:int}/main")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
    typeof(UploadBookImagesResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadBookImagesResponse>>
    SetMainBookImage(
        int bookId,
        int bookImageId,
        CancellationToken cancellationToken)
    {
        UploadBookImagesResponse response =
            await _bookService.SetMainBookImageAsync(
                bookId,
                bookImageId,
                cancellationToken);

        return Ok(
            MakeImageUrlsAbsolute(response));
    }
    [HttpDelete("{bookId:int}/images/{bookImageId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
    typeof(UploadBookImagesResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadBookImagesResponse>>
    DeleteBookImage(
        int bookId,
        int bookImageId,
        CancellationToken cancellationToken)
    {
        UploadBookImagesResponse response =
            await _bookService.DeleteBookImageAsync(
                bookId,
                bookImageId,
                cancellationToken);

        return Ok(
            MakeImageUrlsAbsolute(response));
    }
    private string ToAbsoluteUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return imageUrl;
        }

        if (Uri.TryCreate(
                imageUrl,
                UriKind.Absolute,
                out Uri? absoluteUri))
        {
            return absoluteUri.ToString();
        }

        string normalizedPath =
            imageUrl.StartsWith('/')
                ? imageUrl
                : $"/{imageUrl}";

        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{normalizedPath}";
    }

    private BookDetailsResponse MakeImageUrlsAbsolute(
        BookDetailsResponse response)
    {
        foreach (BookImageResponse image in response.Images)
        {
            image.ImageUrl =
                ToAbsoluteUrl(image.ImageUrl);
        }

        return response;
    }

    private UploadBookImagesResponse MakeImageUrlsAbsolute(
        UploadBookImagesResponse response)
    {
        foreach (BookImageResponse image in response.Images)
        {
            image.ImageUrl =
                ToAbsoluteUrl(image.ImageUrl);
        }

        return response;
    }
}