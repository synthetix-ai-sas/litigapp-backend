using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Api.IntegrationTests.Common;

/// <summary>Never hits Resend from integration tests — mirrors FakeRamaJudicialClient's role.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    public Task<Result<string>> SendAsync(
        string toEmail, string subject, string htmlBody, string? idempotencyKey = null, CancellationToken ct = default) =>
        Task.FromResult(Result<string>.Success("fake-email-id"));
}
