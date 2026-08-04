namespace ElectronicLibrary.DAL.DTOs.Responses.Books;

public sealed class UploadBookImagesResponse
{
    public int BookId { get; set; }

    public IReadOnlyCollection<BookImageResponse> Images { get; set; }
        = Array.Empty<BookImageResponse>();
}