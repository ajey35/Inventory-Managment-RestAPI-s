using Inventory.Api.Models;
using Inventory.Api.Storage;

namespace Inventory.Tests;

public sealed class InMemoryProductStoreTests
{
    [Fact]
    public async Task AdjustStockAsync_DoesNotAllowNegativeStock()
    {
        var store = new InMemoryProductStore();
        var product = new Product(Guid.NewGuid(), "Desk lamp", "LAMP-1", 24.99m, 2, Guid.NewGuid());
        await store.TryAddAsync(product, CancellationToken.None);

        var result = await store.AdjustStockAsync(product.Id, -3, CancellationToken.None);
        var unchanged = await store.GetByIdAsync(product.Id, CancellationToken.None);

        Assert.Equal(StockAdjustmentStatus.InsufficientStock, result.Status);
        Assert.Equal(2, unchanged!.StockQuantity);
    }

    [Fact]
    public async Task TryAddAsync_RejectsDuplicateSkuIgnoringCase()
    {
        var store = new InMemoryProductStore();
        var first = new Product(Guid.NewGuid(), "Desk lamp", "LAMP-1", 24.99m, 2, Guid.NewGuid());
        var duplicate = first with { Id = Guid.NewGuid(), Sku = "lamp-1" };

        Assert.True(await store.TryAddAsync(first, CancellationToken.None));
        Assert.False(await store.TryAddAsync(duplicate, CancellationToken.None));
    }
}
