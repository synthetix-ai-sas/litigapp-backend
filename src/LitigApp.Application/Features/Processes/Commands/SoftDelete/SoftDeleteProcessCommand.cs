using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Processes.Commands.SoftDelete;

public sealed record SoftDeleteProcessCommand(Guid Id) : ICommand<Unit>;
