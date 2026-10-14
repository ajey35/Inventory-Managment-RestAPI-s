namespace Inventory.Api.DTOs;

public sealed record UpdateProductRequest(
    string? Name,
    string? Sku,
    decimal Price,
    int StockQuantity,
    Guid CategoryId);
