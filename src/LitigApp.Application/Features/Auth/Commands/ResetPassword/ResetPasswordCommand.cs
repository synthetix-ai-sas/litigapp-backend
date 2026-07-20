using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Uid, string Token, string NewPassword) : ICommand<Unit>;
