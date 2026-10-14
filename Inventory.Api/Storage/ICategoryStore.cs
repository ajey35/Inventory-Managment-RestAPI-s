using Inventory.Api.Models;

namespace Inventory.Api.Storage;

public interface ICategoryStore
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> TryAddAsync(Category category, CancellationToken cancellationToken);

    Task<bool> TryUpdateAsync(Category category, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
