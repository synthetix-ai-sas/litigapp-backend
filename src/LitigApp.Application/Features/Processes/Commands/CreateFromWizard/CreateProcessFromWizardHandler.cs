using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Application.Features.Processes.Services;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Processes.Commands.CreateFromWizard;

public sealed class CreateProcessFromWizardHandler(
    IProcessRepository repository,
    ProcessCreationService creation)
    : ICommandHandler<CreateProcessFromWizardCommand, ProcessDetailDto>
{
    public async Task<Result<ProcessDetailDto>> HandleAsync(
        CreateProcessFromWizardCommand command, CancellationToken ct = default)
    {
        var court = await repository.FindCourtAsync(command.CourtId, ct);
        if (court is null || court.CityId != command.CityId)
            return Result<ProcessDetailDto>.Failure(ProcessErrorCodes.CourtNotFound);

        var composed = FileNumberRules.Compose(court.OfficialCode, command.FilingYear, command.Consecutive);
        if (!composed.IsSuccess)
            return Result<ProcessDetailDto>.Failure(composed.Error!);

        return await creation.CreateAsync(composed.Value!, command.Alias, ct);
    }
}
