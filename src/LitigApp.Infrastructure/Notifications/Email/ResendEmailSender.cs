using System.Text.RegularExpressions;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace LitigApp.Infrastructure.Notifications.Email;

/// <summary>
/// Sends via the official Resend .NET SDK (<see cref="IResend"/>) — never raw HttpClient.
/// <see cref="ResendClientOptions.ThrowExceptions"/> defaults to true, so a failed send
/// throws <see cref="ResendException"/>; that's caught here and mapped to
/// <see cref="Result{T}.Failure"/> so callers never need to catch provider exceptions.
/// </summary>
internal sealed class ResendEmailSender(
    IResend resend,
    IOptions<ResendSenderOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    // Crude but sufficient plain-text fallback — good enough for spam-filter / accessibility
    // purposes; the HTML body is the actual rendered content the recipient sees.
    private static readonly Regex TagStripper = new("<[^>]+>", RegexOptions.Compiled);

    public async Task<Result<string>> SendAsync(
        string toEmail, string subject, string htmlBody, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var opts = options.Value;
        var recipient = string.IsNullOrWhiteSpace(opts.DevRedirectTo) ? toEmail : opts.DevRedirectTo;

        var message = new EmailMessage
        {
            From = $"{opts.FromName} <{opts.FromAddress}>",
            To = recipient,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = TagStripper.Replace(htmlBody, string.Empty).Trim(),
        };

        try
        {
            var response = idempotencyKey is null
                ? await resend.EmailSendAsync(message, ct)
                : await resend.EmailSendAsync(idempotencyKey, message, ct);

            return Result<string>.Success(response.Content.ToString());
        }
        catch (Exception ex)
        {
            // No PII in logs: never log htmlBody or the recipient address, subject is safe
            // (blueprint subjects carry only counts, e.g. "Tienes 3 novedades...").
            logger.LogError(ex, "Resend send failed for subject '{Subject}'.", subject);
            return Result<string>.Failure(ex.Message);
        }
    }
}
