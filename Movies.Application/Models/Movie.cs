using System.Text.RegularExpressions;

namespace Movies.Application.Models;

public partial class Movie
{
    public required Guid Id { get; init; } //init only can not change 

    public required string Title { get; set; }

    public string Slug => GenerateSlug();



    public required int YearOfRelease { get; set; }

    public required List<string> Genres { get; init; } = new();


    private string GenerateSlug()
    {
        // Use the generated static method for regex replacement
        var sluggedTitle = SlugRegex().Replace(Title, string.Empty) // ✔ Removes special characters except letters, numbers, _, and -.
            .ToLower() // ✔ Converts the title to lowercase.
            .Replace(" ", "-"); // ✔ Replaces spaces with - for a URL-friendly format.
        return $"{sluggedTitle}-{YearOfRelease}";
    }

    [GeneratedRegex("[^0-9A-Za-z _-]", RegexOptions.NonBacktracking, 5)]
    private static partial Regex SlugRegex();
}
