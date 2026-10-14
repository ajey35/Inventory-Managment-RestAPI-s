namespace Inventory.Api.Models;

public sealed record Product(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    int StockQuantity,
    Guid CategoryId);
