namespace LitigApp.Application.Common.Models;

/// <summary>
/// Standard paginated envelope used by list endpoints.
/// Matches the blueprint shape: { items, total, page, pageSize, totalPages }.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
