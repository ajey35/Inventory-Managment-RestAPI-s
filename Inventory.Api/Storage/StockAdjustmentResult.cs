using Inventory.Api.Models;

namespace Inventory.Api.Storage;

public enum StockAdjustmentStatus
{
    Success,
    NotFound,
    InsufficientStock,
    QuantityOverflow
}

public sealed record StockAdjustmentResult(StockAdjustmentStatus Status, Product? Product);
