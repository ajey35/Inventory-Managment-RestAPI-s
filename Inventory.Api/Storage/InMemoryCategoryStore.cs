using Inventory.Api.Models;

namespace Inventory.Api.Storage;

public sealed class InMemoryCategoryStore : ICategoryStore
{
    private readonly Dictionary<Guid, Category> _categories = [];
    private readonly object _sync = new();

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<Category>>(_categories.Values.ToArray());
        }
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _categories.TryGetValue(id, out var category);
            return Task.FromResult(category);
        }
    }

    public Task<bool> TryAddAsync(Category category, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_categories.Values.Any(existing =>
                    string.Equals(existing.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            _categories.Add(category.Id, category);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryUpdateAsync(Category category, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (!_categories.ContainsKey(category.Id) ||
                _categories.Values.Any(existing =>
                    existing.Id != category.Id &&
                    string.Equals(existing.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            _categories[category.Id] = category;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult(_categories.Remove(id));
        }
    }
}
