using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Models.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.DAL.Seed;

public static class CatalogSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedCategoriesAsync(context);

        await SeedAuthorsAsync(context);

        await SeedPublishersAsync(context);

        await SeedBooksAsync(context);
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var categories = new List<Category>
        {
            new()
            {
                Name = "Technology",
                Description = "Programming, software, and technology books."
            },
            new()
            {
                Name = "Science",
                Description = "Books about science and scientific topics."
            },
            new()
            {
                Name = "Literature",
                Description = "Novels, poetry, and literary works."
            },
            new()
            {
                Name = "Business",
                Description = "Business, management, and entrepreneurship books."
            },
            new()
            {
                Name = "History",
                Description = "Historical books and references."
            }
        };

        await context.Categories.AddRangeAsync(categories);

        await context.SaveChangesAsync();
    }

    private static async Task SeedAuthorsAsync(ApplicationDbContext context)
    {
        if (await context.Authors.AnyAsync())
        {
            return;
        }

        var authors = new List<Author>
        {
            new()
            {
                Name = "Robert C. Martin",
                Biography = "Software engineer and author."
            },
            new()
            {
                Name = "George Orwell",
                Biography = "English novelist and essayist."
            },
            new()
            {
                Name = "James Clear",
                Biography = "Author and speaker."
            }
        };

        await context.Authors.AddRangeAsync(authors);

        await context.SaveChangesAsync();
    }

    private static async Task SeedPublishersAsync(ApplicationDbContext context)
    {
        if (await context.Publishers.AnyAsync())
        {
            return;
        }

        var publishers = new List<Publisher>
        {
            new()
            {
                Name = "Pearson"
            },
            new()
            {
                Name = "Penguin Books"
            },
            new()
            {
                Name = "Prentice Hall"
            }
        };

        await context.Publishers.AddRangeAsync(publishers);

        await context.SaveChangesAsync();
    }

    private static async Task SeedBooksAsync(ApplicationDbContext context)
    {
        if (await context.Books.AnyAsync())
        {
            return;
        }

        var publisher = await context.Publishers.FirstAsync();

        var author = await context.Authors.FirstAsync();

        var category = await context.Categories.FirstAsync();

        var book = new Book
        {
            Title = "Clean Code",
            Isbn = "9780132350884",
            Description = "A handbook of agile software craftsmanship.",
            Language = "English",
            PublicationYear = 2008,
            PublisherId = publisher.PublisherId,
            BookAuthors =
            [
                new BookAuthor
                {
                    AuthorId = author.AuthorId
                }
            ],
            BookCategories =
            [
                new BookCategory
                {
                    CategoryId = category.CategoryId
                }
            ]
        };

        await context.Books.AddAsync(book);

        await context.SaveChangesAsync();
    }
}