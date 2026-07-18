using System.Text.Json.Serialization;

namespace LitigApp.Application.Features.Notifications.Dtos;

/// <summary>
/// Shape of <c>OutboxMessage.Payload</c> for <c>event_type='UserProcessesUpdated'</c>
/// (blueprint §11, notifications_outbox). Stores the FULL changed-process set — the
/// digest's MaxRows cut is a presentation concern applied at render time, not at
/// insert time, so a later fallback-sweep retry still has the complete picture.
/// </summary>
public sealed record UserDigestOutboxPayload(
    [property: JsonPropertyName("processes")] IReadOnlyList<UserDigestProcessPayload> Processes,
    [property: JsonPropertyName("totalProcessesChanged")] int TotalProcessesChanged,
    [property: JsonPropertyName("syncCycleAt")] DateTimeOffset SyncCycleAt);

public sealed record UserDigestProcessPayload(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("fileNumber")] string FileNumber,
    [property: JsonPropertyName("currentStatus")] string? CurrentStatus,
    [property: JsonPropertyName("annotation")] string? Annotation,
    [property: JsonPropertyName("lastActionDate")] DateTimeOffset LastActionDate);
