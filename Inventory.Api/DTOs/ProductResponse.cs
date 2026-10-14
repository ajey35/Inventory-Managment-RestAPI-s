namespace Inventory.Api.DTOs;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    int StockQuantity,
    Guid CategoryId);
