using LitigApp.Domain.Common;

namespace LitigApp.Application.Common.Abstractions;

public interface IEmailSender
{
    Task<Result<string>> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default);
}
