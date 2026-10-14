namespace Inventory.Api.DTOs;

public sealed record CreateProductRequest(
    string? Name,
    string? Sku,
    decimal Price,
    int StockQuantity,
    Guid CategoryId);
