using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Processes.Commands.SoftDelete;

public sealed class SoftDeleteProcessHandler(
    IProcessRepository repository,
    IDateTimeProvider clock,
    ICurrentUserService currentUser)
    : ICommandHandler<SoftDeleteProcessCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(SoftDeleteProcessCommand command, CancellationToken ct = default)
    {
        var process = await repository.GetOwnedAsync(command.Id, currentUser.UserId!, ct);
        if (process is null)
            return Result<Unit>.Failure(ProcessErrorCodes.ProcessNotFound);

        if (process.IsActive)
        {
            process.IsActive = false;
            process.UpdatedAt = clock.UtcNow;
            await repository.SaveChangesAsync(ct);
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
