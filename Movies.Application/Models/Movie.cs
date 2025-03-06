namespace Movies.Application.Models;

public class Movie
{
    public required Guid Id { get; init; } //init only can not change 
    
    public required string Title { get; set; }
    
    public required int YearOfRelease { get; set; }
    
    public required List<string> Genres { get; init; } = new();
}
