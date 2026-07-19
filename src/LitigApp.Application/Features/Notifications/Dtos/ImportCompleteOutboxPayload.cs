using System.Text.Json.Serialization;

namespace LitigApp.Application.Features.Notifications.Dtos;

/// <summary>
/// Shape of <c>OutboxMessage.Payload</c> for <c>event_type='ImportComplete'</c>
/// (blueprint §11, notifications_outbox). Written by BulkImportJob, read back by
/// NotificationDispatchService — explicit property names keep both sides in sync
/// regardless of C# naming.
/// </summary>
public sealed record ImportCompleteOutboxPayload(
    [property: JsonPropertyName("importJobId")] Guid ImportJobId,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("totalRows")] int TotalRows,
    [property: JsonPropertyName("successCount")] int SuccessCount,
    [property: JsonPropertyName("duplicateCount")] int DuplicateCount,
    [property: JsonPropertyName("errorCount")] int ErrorCount,
    [property: JsonPropertyName("completedAt")] DateTimeOffset CompletedAt);
