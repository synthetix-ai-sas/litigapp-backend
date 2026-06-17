using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Features.Processes.Queries.ListProcesses;

public sealed record ListProcessesQuery(
    int Page = 1,
    int PageSize = 20,
    string? CourtName = null,
    string? FileNumber = null,
    string? SubjectName = null,
    string? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    bool? Attended = null) : IQuery<PagedResult<ProcessListItemDto>>;
