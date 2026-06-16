using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Application.Features.Processes.Services;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Processes.Commands.CreateFromFileNumber;

public sealed class CreateProcessFromFileNumberHandler(ProcessCreationService creation)
    : ICommandHandler<CreateProcessFromFileNumberCommand, ProcessDetailDto>
{
    public Task<Result<ProcessDetailDto>> HandleAsync(
        CreateProcessFromFileNumberCommand command, CancellationToken ct = default)
        => creation.CreateAsync(command.FileNumber, command.Alias, ct);
}
