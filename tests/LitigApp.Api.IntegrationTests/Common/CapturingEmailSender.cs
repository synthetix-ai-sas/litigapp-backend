using System.Collections.Concurrent;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Api.IntegrationTests.Common;

/// <summary>
/// Records every email sent during a test run so tests can assert on subject/body
/// and extract reset tokens without calling the real Resend API.
/// </summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public record SentEmail(string To, string Subject, string HtmlBody);

    public ConcurrentQueue<SentEmail> SentEmails { get; } = new();

    public Task<Result<string>> SendAsync(
        string toEmail, string subject, string htmlBody, string? idempotencyKey = null, CancellationToken ct = default)
    {
        SentEmails.Enqueue(new SentEmail(toEmail, subject, htmlBody));
        return Task.FromResult(Result<string>.Success("fake-email-id"));
    }
}
