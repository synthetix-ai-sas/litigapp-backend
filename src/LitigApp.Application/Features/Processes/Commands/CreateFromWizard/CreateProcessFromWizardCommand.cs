using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Features.Processes.Commands.CreateFromWizard;

public sealed record CreateProcessFromWizardCommand(
    string CityId,
    Guid CourtId,
    int FilingYear,
    string Consecutive,
    string? Alias = null) : ICommand<ProcessDetailDto>;
