using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListSpecialties;

public sealed record ListSpecialtiesQuery : IQuery<List<SpecialtyDto>>;
