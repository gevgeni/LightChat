using FluentValidation;
using LightChat.Web.Models;

namespace LightChat.Web.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Имя пользователя не может быть пустым.")
                .MinimumLength(3).WithMessage("Имя пользователя должно быть не менее 3 символов.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль не может быть пустым.")
                .MinimumLength(6).WithMessage("Пароль должен быть не менее 6 символов");
        }
    }
}
