namespace LitigApp.Application.Features.Catalog.Dtos;

public sealed record CourtDto(
    Guid Id,
    string OfficialCode,
    string Name,
    string? EntityCode,
    string? SpecialtyCode,
    short? CourtNumber);
