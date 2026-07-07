using FluentValidation;
using LightChat.Web.Models;

namespace LightChat.Web.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Имя пользователя не может быть пустым.")
                .MinimumLength(3).WithMessage("Имя пользователя должно быть не менее 3 символов.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email не может быть пустым.")
                .EmailAddress().WithMessage("Некорректный формат Email.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль не может быть пустым.")
                .MinimumLength(6).WithMessage("Пароль должен быть не менее 6 символов");
        }
    }
}
