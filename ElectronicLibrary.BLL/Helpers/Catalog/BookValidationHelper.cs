using ElectronicLibrary.BLL.Exceptions;

namespace ElectronicLibrary.BLL.Helpers.Catalog;

internal static class BookValidationHelper
{
    public static string NormalizeRequiredText(
        string? value,
        string errorKey)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new InvalidOperationException(errorKey);
        }

        return normalizedValue;
    }

    public static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    public static string? NormalizeIsbn(
        string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        string trimmedIsbn = isbn.Trim();

        bool containsInvalidCharacters =
            trimmedIsbn.Any(character =>
                !char.IsDigit(character) &&
                character != '-' &&
                !char.IsWhiteSpace(character));

        if (containsInvalidCharacters)
        {
            throw new InvalidOperationException(
                "InvalidIsbn");
        }

        string normalizedIsbn = new(
            trimmedIsbn
                .Where(char.IsDigit)
                .ToArray());

        if (normalizedIsbn.Length is not 10 and not 13)
        {
            throw new InvalidOperationException(
                "InvalidIsbn");
        }

        return normalizedIsbn;
    }

    public static void ValidatePublicationYear(
        int? publicationYear)
    {
        if (!publicationYear.HasValue)
        {
            return;
        }

        if (publicationYear.Value < 1 ||
            publicationYear.Value > DateTime.UtcNow.Year)
        {
            throw new InvalidOperationException(
                "InvalidPublicationYear");
        }
    }

    public static IReadOnlyCollection<int> ValidateAuthorIds(
        ICollection<int>? authorIds)
    {
        if (authorIds is null || authorIds.Count == 0)
        {
            throw new InvalidOperationException(
                "BookAuthorsRequired");
        }

        if (authorIds.Any(authorId => authorId <= 0))
        {
            throw new InvalidOperationException(
                "OneOrMoreAuthorsNotFound");
        }

        int[] distinctAuthorIds =
            authorIds.Distinct().ToArray();

        if (distinctAuthorIds.Length != authorIds.Count)
        {
            throw new InvalidOperationException(
                "DuplicateAuthorIds");
        }

        return distinctAuthorIds;
    }

    public static IReadOnlyCollection<int> ValidateCategoryIds(
        ICollection<int>? categoryIds)
    {
        if (categoryIds is null || categoryIds.Count == 0)
        {
            throw new InvalidOperationException(
                "BookCategoriesRequired");
        }

        if (categoryIds.Any(categoryId => categoryId <= 0))
        {
            throw new InvalidOperationException(
                "OneOrMoreCategoriesNotFound");
        }

        int[] distinctCategoryIds =
            categoryIds.Distinct().ToArray();

        if (distinctCategoryIds.Length != categoryIds.Count)
        {
            throw new InvalidOperationException(
                "DuplicateCategoryIds");
        }

        return distinctCategoryIds;
    }
}