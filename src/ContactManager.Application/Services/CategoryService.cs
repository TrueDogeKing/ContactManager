using ContactManager.Application.DTOs.Categories;
using ContactManager.Application.Interfaces;
using ContactManager.Domain.Entities;
using ContactManager.Domain.Repositories;

namespace ContactManager.Application.Services;

/// Implementation of ICategoryService: reads categories and maps entities to DTOs.
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;

    /// Creates service with dependencies.
    public CategoryService(ICategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<IReadOnlyList<CategoryResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        var categories = await _categories.GetAllWithSubcategoriesAsync(cancellationToken);
        return categories.Select(ToResponse).ToList();
    }

    /// Maps a category entity (with subcategories) to its API representation.
    private static CategoryResponseDto ToResponse(Category category) =>
        new(
            category.Id,
            category.Name,
            category.AllowsCustomSubcategory,
            category
                .Subcategories.OrderBy(s => s.Id)
                .Select(s => new SubcategoryResponseDto(s.Id, s.Name))
                .ToList()
        );
}
