using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string ResetToken, string NewPassword) : ICommand<Unit>;
