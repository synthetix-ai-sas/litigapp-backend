namespace LitigApp.Application.Features.Processes.Dtos;

/// <summary>Row shown in the Novedades / Procesos lists.</summary>
public sealed record ProcessListItemDto(
    Guid Id,
    string FileNumber,
    string? Alias,
    string? CurrentStatus,
    DateTimeOffset? LastCourtActionAt,
    string? CourtName,
    bool Attended);
