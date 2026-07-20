using LitigApp.Domain.Common;

namespace LitigApp.Application.Common.Abstractions;

/// <summary>An email attachment as raw bytes — never written to disk (blueprint §9 CSV de errores).</summary>
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

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
        IReadOnlyList<EmailAttachment>? attachments = null,
        CancellationToken ct = default);
}
