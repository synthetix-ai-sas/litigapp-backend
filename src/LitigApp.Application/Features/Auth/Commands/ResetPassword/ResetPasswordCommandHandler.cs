using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IIdentityService identityService,
    IAuthRepository authRepository) : ICommandHandler<ResetPasswordCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(ResetPasswordCommand command, CancellationToken ct = default)
    {
        var opResult = await identityService.ResetPasswordByUserIdAsync(
            command.Uid, command.Token, command.NewPassword, ct);

        if (!opResult.Succeeded)
            return Result<Unit>.Failure(opResult.Error!);

        // Force re-login on all devices — the password changed, old refresh tokens are stale.
        await authRepository.RevokeAllUserRefreshTokensAsync(opResult.UserId!, ct);
        await authRepository.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
