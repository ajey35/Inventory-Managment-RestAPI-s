namespace Inventory.Api.DTOs;

public sealed record ProductListQuery
{
    public string? Search { get; init; }

    public Guid? CategoryId { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public string SortBy { get; init; } = "name";

    public string SortOrder { get; init; } = "asc";

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
