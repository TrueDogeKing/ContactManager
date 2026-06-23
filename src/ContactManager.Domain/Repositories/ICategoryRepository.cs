using ContactManager.Domain.Entities;

namespace ContactManager.Domain.Repositories;

/// Read-only access to category dictionary data.
public interface ICategoryRepository
{
    /// Returns all categories with their subcategories (read-only).
    Task<IReadOnlyList<Category>> GetAllWithSubcategoriesAsync(CancellationToken cancellationToken = default);

    /// Returns the category with the given id including its subcategories, or null (read-only).
    Task<Category?> GetByIdWithSubcategoriesAsync(int id, CancellationToken cancellationToken = default);
}
