using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : ICommand<AuthTokensResponse>;
