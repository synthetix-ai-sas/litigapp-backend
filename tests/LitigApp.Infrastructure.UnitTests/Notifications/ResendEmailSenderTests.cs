using System.Net;
using LitigApp.Infrastructure.Notifications.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Resend;
using EmailAttachment = LitigApp.Application.Common.Abstractions.EmailAttachment;

namespace LitigApp.Infrastructure.UnitTests.Notifications;

public class ResendEmailSenderTests
{
    private readonly IResend _resend = Substitute.For<IResend>();

    private ResendEmailSender CreateSut(ResendSenderOptions? opts = null) =>
        new(_resend, Options.Create(opts ?? new ResendSenderOptions
        {
            FromAddress = "contac@notifications.synthetixaisas.com",
            FromName = "LitigApp",
        }), NullLogger<ResendEmailSender>.Instance);

    [Fact]
    public async Task SendAsync_Success_ReturnsProviderMessageId()
    {
        var id = Guid.NewGuid();
        _resend.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(new ResendResponse<Guid>(id, null!));

        var result = await CreateSut().SendAsync("lawyer@example.com", "Asunto", "<p>hi</p>");

        Assert.True(result.IsSuccess);
        Assert.Equal(id.ToString(), result.Value);
    }

    [Fact]
    public async Task SendAsync_WithIdempotencyKey_UsesTheKeyedOverload()
    {
        _resend.EmailSendAsync("outbox-1", Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(new ResendResponse<Guid>(Guid.NewGuid(), null!));

        await CreateSut().SendAsync("lawyer@example.com", "Asunto", "<p>hi</p>", idempotencyKey: "outbox-1");

        await _resend.Received(1).EmailSendAsync("outbox-1", Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _resend.DidNotReceive().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_DevRedirectToSet_OverridesRecipient()
    {
        EmailMessage? captured = null;
        _resend.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<EmailMessage>();
                return Task.FromResult(new ResendResponse<Guid>(Guid.NewGuid(), null!));
            });

        var sut = CreateSut(new ResendSenderOptions
        {
            FromAddress = "contac@notifications.synthetixaisas.com",
            FromName = "LitigApp",
            DevRedirectTo = "dev-inbox@example.com",
        });

        await sut.SendAsync("real-lawyer@example.com", "Asunto", "<p>hi</p>");

        Assert.NotNull(captured);
        Assert.Contains(captured!.To, a => a.Email == "dev-inbox@example.com");
        Assert.DoesNotContain(captured.To, a => a.Email == "real-lawyer@example.com");
    }

    [Fact]
    public async Task SendAsync_NoDevRedirect_SendsToRealRecipient()
    {
        EmailMessage? captured = null;
        _resend.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<EmailMessage>();
                return Task.FromResult(new ResendResponse<Guid>(Guid.NewGuid(), null!));
            });

        await CreateSut().SendAsync("real-lawyer@example.com", "Asunto", "<p>hi</p>");

        Assert.Contains(captured!.To, a => a.Email == "real-lawyer@example.com");
    }

    [Fact]
    public async Task SendAsync_WithAttachment_MapsToResendAttachment()
    {
        EmailMessage? captured = null;
        _resend.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<EmailMessage>();
                return Task.FromResult(new ResendResponse<Guid>(Guid.NewGuid(), null!));
            });
        var csvBytes = "Fila,Radicado,Motivo\r\n"u8.ToArray();

        await CreateSut().SendAsync("lawyer@example.com", "Asunto", "<p>hi</p>",
            attachments: [new EmailAttachment("procesos_con_errores.csv", "text/csv", csvBytes)]);

        Assert.NotNull(captured);
        var attachment = Assert.Single(captured!.Attachments!);
        Assert.Equal("procesos_con_errores.csv", attachment.Filename);
        Assert.Equal("text/csv", attachment.ContentType);
        Assert.Equal(csvBytes, (byte[])attachment.Content!);
    }

    [Fact]
    public async Task SendAsync_NoAttachments_SendsNoneToResend()
    {
        EmailMessage? captured = null;
        _resend.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<EmailMessage>();
                return Task.FromResult(new ResendResponse<Guid>(Guid.NewGuid(), null!));
            });

        await CreateSut().SendAsync("lawyer@example.com", "Asunto", "<p>hi</p>");

        Assert.NotNull(captured);
        Assert.True(captured!.Attachments is null || captured.Attachments.Count == 0);
    }

    [Fact]
    public async Task SendAsync_ResendThrows_ReturnsFailure_DoesNotThrow()
    {
        _resend.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task<ResendResponse<Guid>>>(_ =>
                throw new ResendException(HttpStatusCode.BadRequest, ErrorType.ValidationError, "boom", null));

        var result = await CreateSut().SendAsync("lawyer@example.com", "Asunto", "<p>hi</p>");

        Assert.False(result.IsSuccess);
    }
}
