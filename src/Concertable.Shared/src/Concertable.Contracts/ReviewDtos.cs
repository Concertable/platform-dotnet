namespace Concertable.Contracts;

public sealed record ReviewDto
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public int Stars { get; init; }
    public string? Details { get; init; }
}

public sealed record ReviewSummary(int TotalReviews, double? AverageRating);
