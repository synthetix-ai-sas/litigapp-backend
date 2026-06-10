using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListEntities;

public sealed record ListEntitiesQuery : IQuery<List<EntityDto>>;
