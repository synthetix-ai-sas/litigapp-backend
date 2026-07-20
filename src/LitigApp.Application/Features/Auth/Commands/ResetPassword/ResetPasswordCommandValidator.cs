using FluentValidation;

namespace LitigApp.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Uid).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"\d").WithMessage("La contraseña debe contener al menos un dígito.")
            .Matches(@"[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.");
    }
}
