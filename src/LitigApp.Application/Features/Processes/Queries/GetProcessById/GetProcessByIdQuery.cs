using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Features.Processes.Queries.GetProcessById;

public sealed record GetProcessByIdQuery(Guid Id) : IQuery<ProcessDetailDto?>;
