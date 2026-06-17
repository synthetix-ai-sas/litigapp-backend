using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Read-side access to a user's processes. Returns DTOs (never entities or IQueryable)
/// to keep Application free of EF concerns. All queries are user-scoped and AsNoTracking.
/// </summary>
public interface IProcessReader
{
    /// <summary>Processes with unattended novelties (attended = false), most recent first.</summary>
    Task<PagedResult<ProcessListItemDto>> ListNoveltiesAsync(
        string userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Active processes for the user, with optional filters, most recent first.</summary>
    Task<PagedResult<ProcessListItemDto>> ListAsync(
        string userId, int page, int pageSize, ProcessListFilter filter, CancellationToken ct = default);

    /// <summary>Full detail of a single process owned by the user, or null if not found.</summary>
    Task<ProcessDetailDto?> GetByIdAsync(string userId, Guid id, CancellationToken ct = default);
}
