namespace LitigApp.Application.Common.Abstractions;

/// <summary>Structured failure returned by IRamaJudicialClient methods.</summary>
public sealed record RamaJudicialFailure(FailureKind Kind, string Message);
