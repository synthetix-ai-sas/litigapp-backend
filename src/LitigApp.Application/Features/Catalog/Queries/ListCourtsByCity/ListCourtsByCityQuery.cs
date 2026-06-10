using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListCourtsByCity;

public sealed record ListCourtsByCityQuery(
    string CityId,
    string? SpecialtyCode = null,
    string? EntityCode = null) : IQuery<List<CourtDto>>;
