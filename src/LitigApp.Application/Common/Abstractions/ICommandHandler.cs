using LitigApp.Domain.Common;

namespace LitigApp.Application.Common.Abstractions;

public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct = default);
}
