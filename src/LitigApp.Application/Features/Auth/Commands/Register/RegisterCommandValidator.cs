using FluentValidation;

namespace LitigApp.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.FullName)
            .NotEmpty();

        When(x => x.WhatsAppPhone is not null, () =>
        {
            RuleFor(x => x.WhatsAppPhone!)
                .Matches(@"^\+57[3][0-9]{9}$")
                .WithMessage("WhatsAppPhone must be a valid Colombian mobile number in E.164 format (+57 followed by 10 digits starting with 3).");
        });
    }
}
