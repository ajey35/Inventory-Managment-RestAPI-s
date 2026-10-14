using Inventory.Api.Models;

namespace Inventory.Api.Storage;

public interface IProductStore
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> TryAddAsync(Product product, CancellationToken cancellationToken);

    Task<bool> TryUpdateAsync(Product product, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<StockAdjustmentResult> AdjustStockAsync(
        Guid id,
        int adjustment,
        CancellationToken cancellationToken);
}
