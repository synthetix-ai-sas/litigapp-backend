using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.SearchCourts;

public sealed record SearchCourtsQuery(string NameLike, string? CityId = null) : IQuery<List<CourtDto>>;
