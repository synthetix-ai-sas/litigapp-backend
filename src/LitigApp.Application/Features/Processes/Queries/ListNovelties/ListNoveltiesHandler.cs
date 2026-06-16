using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Features.Processes.Queries.ListNovelties;

public sealed class ListNoveltiesHandler
    : IQueryHandler<ListNoveltiesQuery, PagedResult<ProcessListItemDto>>
{
    private readonly IProcessReader _reader;
    private readonly ICurrentUserService _currentUser;

    public ListNoveltiesHandler(IProcessReader reader, ICurrentUserService currentUser)
    {
        _reader = reader;
        _currentUser = currentUser;
    }

    public Task<PagedResult<ProcessListItemDto>> HandleAsync(
        ListNoveltiesQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        return _reader.ListNoveltiesAsync(_currentUser.UserId!, page, pageSize, ct);
    }
}
