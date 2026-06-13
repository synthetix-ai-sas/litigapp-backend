using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Auth.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand, Unit>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailSender _emailSender;

    public RequestPasswordResetCommandHandler(IIdentityService identityService, IEmailSender emailSender)
    {
        _identityService = identityService;
        _emailSender = emailSender;
    }

    public async Task<Result<Unit>> HandleAsync(RequestPasswordResetCommand command, CancellationToken ct = default)
    {
        var resetToken = await _identityService.GetPasswordResetTokenAsync(command.Email, ct);

        if (resetToken is not null)
        {
            await _emailSender.SendAsync(
                command.Email,
                "Restablecer contraseña — LitigApp",
                $"<p>Tu token de restablecimiento de contraseña es: <strong>{resetToken}</strong></p>",
                ct);
        }

        // Always return success to avoid leaking whether the email is registered
        return Result<Unit>.Success(Unit.Value);
    }
}
