using ContactManager.Application.DTOs.Categories;
using ContactManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Api.Controllers;

/// Category dictionary endpoints. Public, read-only.
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    /// Creates controller with dependencies.
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// Returns all categories with their subcategories. Public endpoint.
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(cancellationToken);
        return Ok(categories);
    }
}
