using ContactManager.Domain.Entities;
using ContactManager.Domain.Repositories;
using ContactManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Repositories;

/// Implementation of ICategoryRepository backed by AppDbContext.
public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    /// Creates repository with database context.
    public CategoryRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Category>> GetAllWithSubcategoriesAsync(
        CancellationToken cancellationToken = default
    ) =>
        await _db
            .Categories.AsNoTracking()
            .Include(c => c.Subcategories)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public Task<Category?> GetByIdWithSubcategoriesAsync(
        int id,
        CancellationToken cancellationToken = default
    ) =>
        _db
            .Categories.AsNoTracking()
            .Include(c => c.Subcategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
