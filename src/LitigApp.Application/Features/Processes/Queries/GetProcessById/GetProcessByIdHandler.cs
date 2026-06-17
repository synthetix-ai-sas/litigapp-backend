using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Features.Processes.Queries.GetProcessById;

public sealed class GetProcessByIdHandler
    : IQueryHandler<GetProcessByIdQuery, ProcessDetailDto?>
{
    private readonly IProcessReader _reader;
    private readonly ICurrentUserService _currentUser;

    public GetProcessByIdHandler(IProcessReader reader, ICurrentUserService currentUser)
    {
        _reader = reader;
        _currentUser = currentUser;
    }

    public Task<ProcessDetailDto?> HandleAsync(GetProcessByIdQuery query, CancellationToken ct = default)
        => _reader.GetByIdAsync(_currentUser.UserId!, query.Id, ct);
}
