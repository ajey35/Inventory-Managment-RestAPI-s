using Inventory.Api.DTOs;
using Inventory.Api.Models;
using Inventory.Api.Storage;

namespace Inventory.Api.Services;

public sealed class CategoryService(
    ICategoryStore categoryStore,
    IProductStore productStore) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var categories = await categoryStore.GetAllAsync(cancellationToken);
        return categories.Select(ToResponse).ToArray();
    }

    public async Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await categoryStore.GetByIdAsync(id, cancellationToken);
        return category is null
            ? throw NotFound(id)
            : ToResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var category = new Category(Guid.NewGuid(), request.Name!.Trim(), NormalizeDescription(request.Description));

        if (!await categoryStore.TryAddAsync(category, cancellationToken))
        {
            throw Conflict($"A category named '{category.Name}' already exists.");
        }

        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        Guid id,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await categoryStore.GetByIdAsync(id, cancellationToken)
            ?? throw NotFound(id);
        Validate(request);

        var updated = existing with
        {
            Name = request.Name!.Trim(),
            Description = NormalizeDescription(request.Description)
        };

        if (!await categoryStore.TryUpdateAsync(updated, cancellationToken))
        {
            throw Conflict($"A category named '{updated.Name}' already exists.");
        }

        return ToResponse(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await categoryStore.GetByIdAsync(id, cancellationToken) is null)
        {
            throw NotFound(id);
        }

        var products = await productStore.GetAllAsync(cancellationToken);
        if (products.Any(product => product.CategoryId == id))
        {
            throw Conflict("A category cannot be deleted while products are assigned to it.");
        }

        if (!await categoryStore.DeleteAsync(id, cancellationToken))
        {
            throw NotFound(id);
        }
    }

    private static void Validate(CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw BadRequest("Category name is required.");
        }

        if (request.Name.Trim().Length > 100)
        {
            throw BadRequest("Category name must be 100 characters or fewer.");
        }

        if (request.Description?.Length > 500)
        {
            throw BadRequest("Category description must be 500 characters or fewer.");
        }
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static CategoryResponse ToResponse(Category category) =>
        new(category.Id, category.Name, category.Description);

    private static InventoryException NotFound(Guid id) =>
        new(StatusCodes.Status404NotFound, "Category not found", $"Category '{id}' was not found.");

    private static InventoryException Conflict(string detail) =>
        new(StatusCodes.Status409Conflict, "Conflict", detail);

    private static InventoryException BadRequest(string detail) =>
        new(StatusCodes.Status400BadRequest, "Invalid request", detail);
}
