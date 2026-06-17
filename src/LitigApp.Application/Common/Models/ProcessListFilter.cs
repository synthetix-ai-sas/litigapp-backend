namespace LitigApp.Application.Common.Models;

/// <summary>
/// Optional filters for the process list (pestaña "Procesos").
/// All fields are nullable — a null/blank value means "do not filter by that field".
/// </summary>
public sealed record ProcessListFilter(
    string? CourtName = null,
    string? FileNumber = null,
    string? SubjectName = null,
    string? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    bool? Attended = null);
