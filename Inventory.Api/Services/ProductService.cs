using Inventory.Api.DTOs;
using Inventory.Api.Models;
using Inventory.Api.Storage;

namespace Inventory.Api.Services;

public sealed class ProductService(
    IProductStore productStore,
    ICategoryStore categoryStore) : IProductService
{
    public async Task<PagedResponse<ProductResponse>> GetAllAsync(
        ProductListQuery query,
        CancellationToken cancellationToken)
    {
        ValidateListQuery(query);
        var products = await productStore.GetAllAsync(cancellationToken);
        IEnumerable<Product> filtered = products;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            filtered = filtered.Where(product =>
                product.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                product.Sku.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (query.CategoryId is Guid categoryId)
        {
            filtered = filtered.Where(product => product.CategoryId == categoryId);
        }

        if (query.MinPrice is decimal minPrice)
        {
            filtered = filtered.Where(product => product.Price >= minPrice);
        }

        if (query.MaxPrice is decimal maxPrice)
        {
            filtered = filtered.Where(product => product.Price <= maxPrice);
        }

        filtered = ApplySort(filtered, query.SortBy, query.SortOrder);
        var matchingProducts = filtered.ToArray();
        var skip = (long)(query.Page - 1) * query.PageSize;
        var pageProducts = skip > int.MaxValue
            ? []
            : matchingProducts.Skip((int)skip).Take(query.PageSize).Select(ToResponse).ToArray();

        return new PagedResponse<ProductResponse>(
            pageProducts,
            query.Page,
            query.PageSize,
            matchingProducts.Length);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await productStore.GetByIdAsync(id, cancellationToken);
        return product is null
            ? throw NotFound("Product", id)
            : ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        ValidateProduct(request.Name, request.Sku, request.Price, request.StockQuantity);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var product = new Product(
            Guid.NewGuid(),
            request.Name!.Trim(),
            request.Sku!.Trim(),
            request.Price,
            request.StockQuantity,
            request.CategoryId);

        if (!await productStore.TryAddAsync(product, cancellationToken))
        {
            throw Conflict($"A product with SKU '{product.Sku}' already exists.");
        }

        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await productStore.GetByIdAsync(id, cancellationToken)
            ?? throw NotFound("Product", id);
        ValidateProduct(request.Name, request.Sku, request.Price, request.StockQuantity);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var updated = existing with
        {
            Name = request.Name!.Trim(),
            Sku = request.Sku!.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId
        };

        if (!await productStore.TryUpdateAsync(updated, cancellationToken))
        {
            throw Conflict($"A product with SKU '{updated.Sku}' already exists.");
        }

        return ToResponse(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await productStore.DeleteAsync(id, cancellationToken))
        {
            throw NotFound("Product", id);
        }
    }

    public async Task<ProductResponse> AdjustStockAsync(
        Guid id,
        AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Adjustment == 0)
        {
            throw BadRequest("Stock adjustment must not be zero.");
        }

        var result = await productStore.AdjustStockAsync(id, request.Adjustment, cancellationToken);
        return result.Status switch
        {
            StockAdjustmentStatus.Success => ToResponse(result.Product!),
            StockAdjustmentStatus.NotFound => throw NotFound("Product", id),
            StockAdjustmentStatus.InsufficientStock => throw Conflict("Stock cannot be adjusted below zero."),
            StockAdjustmentStatus.QuantityOverflow => throw BadRequest("Stock quantity exceeds the supported range."),
            _ => throw new InvalidOperationException("Unknown stock adjustment result.")
        };
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (await categoryStore.GetByIdAsync(categoryId, cancellationToken) is null)
        {
            throw new InventoryException(
                StatusCodes.Status400BadRequest,
                "Invalid category",
                $"Category '{categoryId}' does not exist.");
        }
    }

    private static void ValidateProduct(string? name, string? sku, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw BadRequest("Product name is required.");
        }

        if (name.Trim().Length > 200)
        {
            throw BadRequest("Product name must be 200 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw BadRequest("Product SKU is required.");
        }

        if (sku.Trim().Length > 64)
        {
            throw BadRequest("Product SKU must be 64 characters or fewer.");
        }

        if (price < 0)
        {
            throw BadRequest("Product price cannot be negative.");
        }

        if (stockQuantity < 0)
        {
            throw BadRequest("Product stock quantity cannot be negative.");
        }
    }

    private static void ValidateListQuery(ProductListQuery query)
    {
        if (query.Page < 1)
        {
            throw BadRequest("Page must be greater than or equal to 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw BadRequest("Page size must be between 1 and 100.");
        }

        if (query.MinPrice < 0 || query.MaxPrice < 0)
        {
            throw BadRequest("Price filters cannot be negative.");
        }

        if (query.MinPrice > query.MaxPrice)
        {
            throw BadRequest("Minimum price cannot exceed maximum price.");
        }

        if (query.SortBy.Trim().ToLowerInvariant() is not ("name" or "sku" or "price" or "stock"))
        {
            throw BadRequest("Sort by must be one of: name, sku, price, stock.");
        }

        if (query.SortOrder.Trim().ToLowerInvariant() is not ("asc" or "desc"))
        {
            throw BadRequest("Sort order must be 'asc' or 'desc'.");
        }
    }

    private static IEnumerable<Product> ApplySort(IEnumerable<Product> products, string sortBy, string sortOrder)
    {
        var descending = sortOrder.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy.Trim().ToLowerInvariant(), descending) switch
        {
            ("name", false) => products.OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase),
            ("name", true) => products.OrderByDescending(product => product.Name, StringComparer.OrdinalIgnoreCase),
            ("sku", false) => products.OrderBy(product => product.Sku, StringComparer.OrdinalIgnoreCase),
            ("sku", true) => products.OrderByDescending(product => product.Sku, StringComparer.OrdinalIgnoreCase),
            ("price", false) => products.OrderBy(product => product.Price),
            ("price", true) => products.OrderByDescending(product => product.Price),
            ("stock", false) => products.OrderBy(product => product.StockQuantity),
            ("stock", true) => products.OrderByDescending(product => product.StockQuantity),
            _ => throw BadRequest("Unsupported sort option.")
        };
    }

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Name, product.Sku, product.Price, product.StockQuantity, product.CategoryId);

    private static InventoryException NotFound(string resource, Guid id) =>
        new(StatusCodes.Status404NotFound, $"{resource} not found", $"{resource} '{id}' was not found.");

    private static InventoryException Conflict(string detail) =>
        new(StatusCodes.Status409Conflict, "Conflict", detail);

    private static InventoryException BadRequest(string detail) =>
        new(StatusCodes.Status400BadRequest, "Invalid request", detail);
}
