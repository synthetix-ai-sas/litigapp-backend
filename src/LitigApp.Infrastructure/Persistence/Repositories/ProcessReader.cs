using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Domain.Processes;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence.Repositories;

internal sealed class ProcessReader(AppDbContext db) : IProcessReader
{
    public Task<PagedResult<ProcessListItemDto>> ListNoveltiesAsync(
        string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Processes
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsActive && !p.Attended);

        return PaginateAsync(query, page, pageSize, ct);
    }

    public Task<PagedResult<ProcessListItemDto>> ListAsync(
        string userId, int page, int pageSize, ProcessListFilter filter, CancellationToken ct = default)
    {
        var query = db.Processes
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.CourtName))
        {
            var pattern = $"%{filter.CourtName.Trim()}%";
            query = query.Where(p => p.Court != null && EF.Functions.ILike(p.Court.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filter.FileNumber))
        {
            var prefix = filter.FileNumber.Trim();
            query = query.Where(p => p.FileNumber.StartsWith(prefix));
        }

        if (!string.IsNullOrWhiteSpace(filter.SubjectName))
        {
            var pattern = $"%{filter.SubjectName.Trim()}%";
            query = query.Where(p => p.Subjects.Any(s => EF.Functions.ILike(s.Name, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var pattern = $"%{filter.Status.Trim()}%";
            query = query.Where(p => p.CurrentStatus != null && EF.Functions.ILike(p.CurrentStatus, pattern));
        }

        if (filter.FromDate is { } from)
        {
            var fromTs = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(p => p.LastCourtActionAt != null && p.LastCourtActionAt >= fromTs);
        }

        if (filter.ToDate is { } to)
        {
            // inclusive of the whole "to" day
            var toTs = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);
            query = query.Where(p => p.LastCourtActionAt != null && p.LastCourtActionAt < toTs);
        }

        if (filter.Attended is { } attended)
            query = query.Where(p => p.Attended == attended);

        return PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<ProcessDetailDto?> GetByIdAsync(string userId, Guid id, CancellationToken ct = default)
    {
        return await db.Processes
            .AsNoTracking()
            .Where(p => p.Id == id && p.UserId == userId && p.IsActive)
            .Select(p => new ProcessDetailDto(
                p.Id,
                p.FileNumber,
                p.CustomAlias,
                p.Court == null
                    ? null
                    : new CourtSummaryDto(
                        p.Court.Id,
                        p.Court.Name,
                        p.Court.City != null ? p.Court.City.Name : null,
                        p.Court.City != null && p.Court.City.Department != null
                            ? p.Court.City.Department.Name
                            : null),
                p.FilingYear,
                p.ProcessType,
                p.ProcessClass,
                p.JudgeName,
                p.CurrentStatus,
                p.LastCourtActionAt,
                p.Attended,
                p.SyncStatus,
                p.SyncPhase,
                p.IsPrivate,
                // Private processes have no subjects/actions to render, so no PDF (blueprint).
                p.SyncStatus == ProcessSyncStatus.Ok && !p.IsPrivate,
                p.Subjects
                    .Select(s => new ProcessSubjectDto(s.SubjectType, s.Name, s.Identification, s.IsSummoned))
                    .ToList(),
                p.Actions
                    .OrderByDescending(a => a.ConsecutiveNumber)
                    .Select(a => new ProcessActionDto(
                        a.Id,
                        a.ConsecutiveNumber,
                        a.ActionDate,
                        a.Action,
                        a.Annotation,
                        a.TermStartDate,
                        a.TermEndDate,
                        a.GroupedWithId))
                    .ToList()))
            .FirstOrDefaultAsync(ct);
    }

    private static async Task<PagedResult<ProcessListItemDto>> PaginateAsync(
        IQueryable<Process> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.LastCourtActionAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProcessListItemDto(
                p.Id,
                p.FileNumber,
                p.CustomAlias,
                p.CurrentStatus,
                p.LastCourtActionAt,
                p.Court != null ? p.Court.Name : null,
                p.Attended,
                p.IsPrivate))
            .ToListAsync(ct);

        return new PagedResult<ProcessListItemDto>(items, total, page, pageSize);
    }
}
