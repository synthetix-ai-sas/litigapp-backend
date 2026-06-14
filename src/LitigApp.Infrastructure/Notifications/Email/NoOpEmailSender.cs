using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;
using Microsoft.Extensions.Logging;

namespace LitigApp.Infrastructure.Notifications.Email;

public sealed class NoOpEmailSender : IEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;

    public NoOpEmailSender(ILogger<NoOpEmailSender> logger) => _logger = logger;

    public Task<Result<string>> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Email sending skipped (not in MVP yet). To={To} Subject={Subject}",
            toEmail, subject);

        return Task.FromResult(Result<string>.Success("noop"));
    }
}
