using System.Net;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;
using Microsoft.Extensions.Options;

namespace LitigApp.Application.Features.Auth.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IIdentityService identityService,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IDateTimeProvider dateTimeProvider,
    IOptions<AuthOptions> authOptions) : ICommandHandler<RequestPasswordResetCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(RequestPasswordResetCommand command, CancellationToken ct = default)
    {
        var resetData = await identityService.GeneratePasswordResetAsync(command.Email, ct);

        if (resetData is not null)
        {
            var opts = authOptions.Value;
            var frontendBase = opts.FrontendBaseUrl.TrimEnd('/');
            var encodedToken = WebUtility.UrlEncode(resetData.Token);
            var resetUrl = $"{frontendBase}/reset-password?token={encodedToken}&uid={resetData.UserId}";

            var model = new Dictionary<string, object?>
            {
                ["AbogadoNombre"] = WebUtility.HtmlEncode(resetData.FullName),
                ["UrlRestablecimiento"] = resetUrl,
                ["MinutosExpiracion"] = opts.TokenLifespanMinutes,
                ["Año"] = dateTimeProvider.UtcNow.Year,
            };

            var htmlBody = templateRenderer.Render(EmailTemplate.PasswordReset, model);

            await emailSender.SendAsync(
                command.Email,
                "Restablecer contraseña — LitigApp",
                htmlBody,
                ct: ct);
        }

        // Always return success — never leak whether the email is registered.
        return Result<Unit>.Success(Unit.Value);
    }
}
