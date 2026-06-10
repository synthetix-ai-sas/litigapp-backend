using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListDepartments;

public sealed record ListDepartmentsQuery : IQuery<List<DepartmentDto>>;
