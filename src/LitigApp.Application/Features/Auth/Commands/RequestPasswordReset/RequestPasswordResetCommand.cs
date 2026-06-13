using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Auth.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email) : ICommand<Unit>;
