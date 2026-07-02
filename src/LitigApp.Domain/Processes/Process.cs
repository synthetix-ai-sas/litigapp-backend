using LitigApp.Domain.Catalog;

namespace LitigApp.Domain.Processes;

public class Process
{
    public Guid Id { get; set; }

    /// <summary>FK to AspNetUsers.Id (text, Identity convention — Rule #32).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Radicado completo: 23 digits stored as char(23).</summary>
    public string FileNumber { get; set; } = string.Empty;

    // External IDs from Rama Judicial
    public long? ExternalProcessId { get; set; }
    public int? ExternalConnectionId { get; set; }

    // Process metadata
    public Guid? CourtId { get; set; }
    public short? FilingYear { get; set; }
    public string? ProcessType { get; set; }
    public string? ProcessClass { get; set; }
    public string? ProcessSubclass { get; set; }
    public string? Resource { get; set; }
    public string? JudgeName { get; set; }
    public string? FilingContent { get; set; }
    public bool IsPrivate { get; set; }
    public string? CustomAlias { get; set; }

    // Sync state
    public string? CurrentStatus { get; set; }
    public DateTimeOffset? LastCourtActionAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset? LastSyncAttemptAt { get; set; }
    public int LastExternalConsecutive { get; set; }
    public string SyncStatus { get; set; } = ProcessSyncStatus.Pending;
    public string SyncPhase { get; set; } = ProcessSyncPhase.PendingInitialFull;
    public string? SyncError { get; set; }
    public int SyncAttempts { get; set; }

    // UX
    public bool Attended { get; set; } = true;

    // Audit / soft-delete
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public Court? Court { get; set; }
    public ICollection<ProcessAction> Actions { get; set; } = [];
    public ICollection<ProcessSubject> Subjects { get; set; } = [];
}
