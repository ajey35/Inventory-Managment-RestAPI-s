using Inventory.Api.DTOs;

namespace Inventory.Api.Services;

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> GetAllAsync(
        ProductListQuery query,
        CancellationToken cancellationToken);

    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductResponse> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductResponse> AdjustStockAsync(
        Guid id,
        AdjustStockRequest request,
        CancellationToken cancellationToken);
}
