using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Processes.Commands.MarkAttended;

public sealed class MarkAttendedHandler(
    IProcessRepository repository,
    IDateTimeProvider clock,
    ICurrentUserService currentUser)
    : ICommandHandler<MarkAttendedCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(MarkAttendedCommand command, CancellationToken ct = default)
    {
        var process = await repository.GetOwnedAsync(command.Id, currentUser.UserId!, ct);
        if (process is null)
            return Result<Unit>.Failure(ProcessErrorCodes.ProcessNotFound);

        if (!process.Attended)
        {
            process.Attended = true;
            process.UpdatedAt = clock.UtcNow;
            await repository.SaveChangesAsync(ct);
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
