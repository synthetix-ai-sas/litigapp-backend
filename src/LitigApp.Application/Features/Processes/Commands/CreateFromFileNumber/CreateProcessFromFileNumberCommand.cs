using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;

namespace LitigApp.Application.Features.Processes.Commands.CreateFromFileNumber;

public sealed record CreateProcessFromFileNumberCommand(string FileNumber, string? Alias = null)
    : ICommand<ProcessDetailDto>;
