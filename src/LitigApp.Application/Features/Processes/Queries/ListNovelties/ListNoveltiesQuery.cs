using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Features.Processes.Queries.ListNovelties;

public sealed record ListNoveltiesQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ProcessListItemDto>>;
