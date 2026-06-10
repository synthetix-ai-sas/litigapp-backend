using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LitigApp.Infrastructure.Catalog;

public sealed class CachedCatalogReader : ICatalogReader
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan LongTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan ShortTtl = TimeSpan.FromMinutes(30);

    public CachedCatalogReader(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public Task<List<DepartmentDto>> ListDepartmentsAsync(CancellationToken ct = default)
        => _cache.GetOrCreateAsync("catalog:departments", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LongTtl;
            return _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentDto(d.Id, d.Name))
                .ToListAsync(ct);
        })!;

    public Task<List<CityDto>> ListCitiesByDepartmentAsync(string departmentId, CancellationToken ct = default)
        => _cache.GetOrCreateAsync($"catalog:cities:{departmentId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LongTtl;
            return _context.Cities
                .AsNoTracking()
                .Where(c => c.DepartmentId == departmentId)
                .OrderBy(c => c.Name)
                .Select(c => new CityDto(c.Id, c.Name))
                .ToListAsync(ct);
        })!;

    public Task<List<SpecialtyDto>> ListSpecialtiesAsync(CancellationToken ct = default)
        => _cache.GetOrCreateAsync("catalog:specialties", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LongTtl;
            return _context.Specialties
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new SpecialtyDto(s.Code, s.Name))
                .ToListAsync(ct);
        })!;

    public Task<List<EntityDto>> ListEntitiesAsync(CancellationToken ct = default)
        => _cache.GetOrCreateAsync("catalog:entities", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LongTtl;
            return _context.Entities
                .AsNoTracking()
                .OrderBy(e => e.Name)
                .Select(e => new EntityDto(e.Code, e.Name))
                .ToListAsync(ct);
        })!;

    public Task<List<CourtDto>> ListCourtsByCityAsync(
        string cityId, string? specialtyCode, string? entityCode, CancellationToken ct = default)
    {
        var key = $"catalog:courts:{cityId}:{specialtyCode ?? "any"}:{entityCode ?? "any"}";
        return _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ShortTtl;
            var query = _context.Courts
                .AsNoTracking()
                .Where(c => c.CityId == cityId && c.IsActive);

            if (specialtyCode is not null)
                query = query.Where(c => c.SpecialtyCode == specialtyCode);

            if (entityCode is not null)
                query = query.Where(c => c.EntityCode == entityCode);

            return query
                .OrderBy(c => c.Name)
                .Select(c => new CourtDto(c.Id, c.OfficialCode, c.Name, c.EntityCode, c.SpecialtyCode, c.CourtNumber))
                .ToListAsync(ct);
        })!;
    }

    public async Task<List<CourtDto>> SearchCourtsAsync(string nameLike, string? cityId, CancellationToken ct = default)
    {
        // Uses GIN trigram index (idx_courts_name_trgm) via the % similarity operator.
        // Not cached — query is dynamic by nature.
        var courts = cityId is null
            ? await _context.Courts
                .FromSqlInterpolated($"SELECT * FROM courts WHERE is_active = true AND name % {nameLike} LIMIT 50")
                .AsNoTracking()
                .ToListAsync(ct)
            : await _context.Courts
                .FromSqlInterpolated($"SELECT * FROM courts WHERE is_active = true AND city_id = {cityId} AND name % {nameLike} LIMIT 50")
                .AsNoTracking()
                .ToListAsync(ct);

        return courts
            .OrderBy(c => c.Name)
            .Select(c => new CourtDto(c.Id, c.OfficialCode, c.Name, c.EntityCode, c.SpecialtyCode, c.CourtNumber))
            .ToList();
    }
}
