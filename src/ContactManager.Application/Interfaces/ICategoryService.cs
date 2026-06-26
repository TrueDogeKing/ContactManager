using ContactManager.Application.DTOs.Categories;

namespace ContactManager.Application.Interfaces;

/// Read access to category dictionary data.
public interface ICategoryService
{
    /// Returns all categories with their subcategories.
    Task<IReadOnlyList<CategoryResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    );
}
