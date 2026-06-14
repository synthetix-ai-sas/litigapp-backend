using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Unit>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService) =>
        _identityService = identityService;

    public async Task<Result<Unit>> HandleAsync(ResetPasswordCommand command, CancellationToken ct = default)
    {
        var opResult = await _identityService.ResetPasswordAsync(
            command.Email, command.ResetToken, command.NewPassword, ct);

        return opResult.Succeeded
            ? Result<Unit>.Success(Unit.Value)
            : Result<Unit>.Failure(opResult.Error!);
    }
}
