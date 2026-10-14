using Inventory.Api.Models;

namespace Inventory.Api.Storage;

public sealed class InMemoryProductStore : IProductStore
{
    private readonly Dictionary<Guid, Product> _products = [];
    private readonly object _sync = new();

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<Product>>(_products.Values.ToArray());
        }
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _products.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }
    }

    public Task<bool> TryAddAsync(Product product, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_products.Values.Any(existing =>
                    string.Equals(existing.Sku, product.Sku, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            _products.Add(product.Id, product);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryUpdateAsync(Product product, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (!_products.ContainsKey(product.Id) ||
                _products.Values.Any(existing =>
                    existing.Id != product.Id &&
                    string.Equals(existing.Sku, product.Sku, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            _products[product.Id] = product;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult(_products.Remove(id));
        }
    }

    public Task<StockAdjustmentResult> AdjustStockAsync(
        Guid id,
        int adjustment,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (!_products.TryGetValue(id, out var product))
            {
                return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.NotFound, null));
            }

            var newQuantity = (long)product.StockQuantity + adjustment;
            if (newQuantity < 0)
            {
                return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.InsufficientStock, null));
            }

            if (newQuantity > int.MaxValue)
            {
                return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.QuantityOverflow, null));
            }

            var updatedProduct = product with { StockQuantity = (int)newQuantity };
            _products[id] = updatedProduct;
            return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.Success, updatedProduct));
        }
    }
}
