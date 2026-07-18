using System.Text.Json;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Domain.Notifications;
using Microsoft.Extensions.Options;

namespace LitigApp.Application.Features.Notifications;

/// <inheritdoc cref="INotificationDispatchService"/>
public sealed class NotificationDispatchService(
    IIdentityService identityService,
    IEmailTemplateRenderer renderer,
    IEmailSender emailSender,
    IOutboxRepository outboxRepo,
    INotificationLogRepository notificationLogRepo,
    IOptions<NotificationsOptions> options,
    IDateTimeProvider clock) : INotificationDispatchService
{
    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        var profile = await identityService.GetUserProfileAsync(message.UserId, ct);
        if (profile is null)
        {
            await FailAsync(message, "user_not_found", ct);
            return;
        }

        IReadOnlyDictionary<string, object?> model;
        EmailTemplate template;
        string subject;
        Guid[] processIds;

        var opts = options.Value;
        var year = clock.UtcNow.Year;

        switch (message.EventType)
        {
            case "UserProcessesUpdated":
            {
                var payload = JsonSerializer.Deserialize<UserDigestOutboxPayload>(message.Payload);
                if (payload is null) { await FailAsync(message, "malformed_payload", ct); return; }

                var changed = payload.Processes
                    .Select(p => new ChangedProcessDto(p.Id, p.FileNumber, p.LastActionDate, p.CurrentStatus, p.Annotation))
                    .ToList();
                var cut = DigestPayloadBuilder.Build(changed, opts.DigestMaxRows);

                template = EmailTemplate.UserDigest;
                model = UserDigestEmailModelBuilder.Build(profile.FullName, cut, $"{opts.AppBaseUrl}/novelties", year);
                subject = UserDigestEmailModelBuilder.BuildSubject(payload.TotalProcessesChanged);
                processIds = payload.Processes.Select(p => p.Id).ToArray();
                break;
            }
            case "ImportComplete":
            {
                var payload = JsonSerializer.Deserialize<ImportCompleteOutboxPayload>(message.Payload);
                if (payload is null) { await FailAsync(message, "malformed_payload", ct); return; }

                template = EmailTemplate.ImportComplete;
                model = ImportCompleteEmailModelBuilder.Build(
                    profile.FullName, payload.FileName, payload.SuccessCount, payload.DuplicateCount,
                    payload.ErrorCount, $"{opts.AppBaseUrl}/processes", year);
                subject = ImportCompleteEmailModelBuilder.BuildSubject(payload.SuccessCount);
                processIds = [];
                break;
            }
            default:
                await FailAsync(message, $"unknown_event_type:{message.EventType}", ct);
                return;
        }

        var html = renderer.Render(template, model);

        // Idempotency key = the outbox row id: a Polly retry or a later fallback-sweep
        // re-attempt of the SAME row never results in a duplicate email server-side.
        var sendResult = await emailSender.SendAsync(profile.Email, subject, html, message.Id.ToString(), ct);

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        if (sendResult.IsSuccess)
        {
            message.Status = "sent";
            message.ProcessedAt = now;
            await outboxRepo.SaveChangesAsync(ct);

            await notificationLogRepo.InsertAsync(new NotificationLog
            {
                Id = Guid.NewGuid(),
                OutboxId = message.Id,
                UserId = message.UserId,
                EventType = message.EventType,
                Channel = message.Channel,
                ProcessIds = processIds,
                ProviderMessageId = sendResult.Value,
                Status = "delivered",
                SentAt = now,
            }, ct);
        }
        else
        {
            // Leave 'pending' (not 'failed') — NotificationFallbackSweepJob retries it.
            message.Status = "pending";
            message.Attempts++;
            message.LastError = sendResult.Error;
            await outboxRepo.SaveChangesAsync(ct);
        }
    }

    private async Task FailAsync(OutboxMessage message, string reason, CancellationToken ct)
    {
        message.Status = "failed";
        message.Attempts++;
        message.LastError = reason;
        await outboxRepo.SaveChangesAsync(ct);
    }
}
