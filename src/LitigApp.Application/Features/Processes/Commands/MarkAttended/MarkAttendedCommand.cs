using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Processes.Commands.MarkAttended;

public sealed record MarkAttendedCommand(Guid Id) : ICommand<Unit>;
