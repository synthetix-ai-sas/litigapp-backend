using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Features.Processes.Queries.ListProcesses;

public sealed class ListProcessesHandler
    : IQueryHandler<ListProcessesQuery, PagedResult<ProcessListItemDto>>
{
    private readonly IProcessReader _reader;
    private readonly ICurrentUserService _currentUser;

    public ListProcessesHandler(IProcessReader reader, ICurrentUserService currentUser)
    {
        _reader = reader;
        _currentUser = currentUser;
    }

    public Task<PagedResult<ProcessListItemDto>> HandleAsync(
        ListProcessesQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var filter = new ProcessListFilter(
            query.CourtName,
            query.FileNumber,
            query.SubjectName,
            query.Status,
            query.FromDate,
            query.ToDate,
            query.Attended);
        return _reader.ListAsync(_currentUser.UserId!, page, pageSize, filter, ct);
    }
}
