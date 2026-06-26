namespace ContactManager.Application.DTOs.Categories;

/// Category representation returned by the API, including its subcategories.
/// AllowsCustomSubcategory tells the client to offer a free-text field instead of the dictionary list.
public record CategoryResponseDto(
    int Id,
    string Name,
    bool AllowsCustomSubcategory,
    IReadOnlyList<SubcategoryResponseDto> Subcategories
);
