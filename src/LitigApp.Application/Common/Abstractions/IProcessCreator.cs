using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Encapsulates the synchronous 4-endpoint creation flow (overview → detail → subjects → actions).
/// Used by HTTP handlers AND background jobs (which supply <paramref name="userId"/> directly,
/// because they run outside an HTTP context and cannot use <see cref="ICurrentUserService"/>).
/// </summary>
public interface IProcessCreator
{
    Task<Result<ProcessDetailDto>> CreateAsync(
        string userId,
        string fileNumber,
        string? alias,
        CancellationToken ct);
}
