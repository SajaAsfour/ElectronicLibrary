using Microsoft.AspNetCore.Http;

namespace ElectronicLibrary.PL.Models.Requests.Books;

public sealed class UploadBookImagesRequest
{
    public List<IFormFile> Images { get; set; } = [];

    public int? MainImageIndex { get; set; }
}