using LitigApp.Domain.Common;

namespace LitigApp.Application.Common.Abstractions;

public interface IEmailSender
{
    /// <summary>
    /// <paramref name="idempotencyKey"/> (e.g. the outbox row id) lets the provider dedupe a
    /// retried send (Polly retry or a later fallback-sweep attempt) so the recipient never
    /// gets the same notification twice.
    /// </summary>
    Task<Result<string>> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? idempotencyKey = null,
        CancellationToken ct = default);
}
