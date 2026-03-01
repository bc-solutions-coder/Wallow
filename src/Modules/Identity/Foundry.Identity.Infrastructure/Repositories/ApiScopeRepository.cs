using Foundry.Identity.Application.Interfaces;
using Foundry.Identity.Domain.Entities;
using Foundry.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Foundry.Identity.Infrastructure.Repositories;

public sealed class ApiScopeRepository : IApiScopeRepository
{
    private readonly IdentityDbContext _context;

    public ApiScopeRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ApiScope>> GetAllAsync(string? category = null, CancellationToken ct = default)
    {
        IQueryable<ApiScope> query = _context.ApiScopes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Code)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ApiScope>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        List<string> codeList = codes.ToList();
        return await _context.ApiScopes
            .Where(x => codeList.Contains(x.Code))
            .ToListAsync(ct);
    }

    public void Add(ApiScope scope)
    {
        _context.ApiScopes.Add(scope);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
