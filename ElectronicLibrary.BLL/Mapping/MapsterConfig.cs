using ElectronicLibrary.DAL.DTOs.Requests.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Authors;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.DTOs.Requests.Publishers;
using ElectronicLibrary.DAL.DTOs.Responses.Publishers;
using Mapster;

namespace ElectronicLibrary.BLL.Mapping;

public sealed class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateAuthorRequest, Author>()
            .Map(
                destination => destination.Name,
                source => source.Name.Trim())
            .Map(
                destination => destination.Biography,
                source => string.IsNullOrWhiteSpace(source.Biography)
                    ? null
                    : source.Biography.Trim())
            .IgnoreNonMapped(true);

        config.NewConfig<UpdateAuthorRequest, Author>()
            .Map(
                destination => destination.Name,
                source => source.Name.Trim())
            .Map(
                destination => destination.Biography,
                source => string.IsNullOrWhiteSpace(source.Biography)
                    ? null
                    : source.Biography.Trim())
            .IgnoreNonMapped(true);

        config.NewConfig<Author, AuthorResponse>()
            .Map(
                destination => destination.BooksCount,
                source => source.BookAuthors.Count);

        config.NewConfig<Book, AuthorBookResponse>();

        config.NewConfig<Author, AuthorDetailsResponse>()
            .Map(
                destination => destination.Books,
                source => source.BookAuthors.Select(
                    bookAuthor => bookAuthor.Book));

        config.NewConfig<CreatePublisherRequest, Publisher>()
    .Map(
        destination => destination.Name,
        source => source.Name.Trim())
    .Map(
        destination => destination.Website,
        source => string.IsNullOrWhiteSpace(source.Website)
            ? null
            : source.Website.Trim())
    .IgnoreNonMapped(true);

        config.NewConfig<UpdatePublisherRequest, Publisher>()
            .Map(
                destination => destination.Name,
                source => source.Name.Trim())
            .Map(
                destination => destination.Website,
                source => string.IsNullOrWhiteSpace(source.Website)
                    ? null
                    : source.Website.Trim())
            .IgnoreNonMapped(true);

        config.NewConfig<Publisher, PublisherResponse>()
            .Map(
                destination => destination.BooksCount,
                source => source.Books.Count);

        config.NewConfig<Book, PublisherBookResponse>();

        config.NewConfig<Publisher, PublisherDetailsResponse>()
            .Map(
                destination => destination.Books,
                source => source.Books);
    }
}