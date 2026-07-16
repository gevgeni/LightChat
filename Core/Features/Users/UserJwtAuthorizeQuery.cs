using MediatR;
using FluentValidation;

namespace LightChat.Core.Features.Users
{
    public record UserJwtAuthorizeQuery(string Username, string Password) : IRequest<JwtTokenDto>;
    public record JwtTokenDto(string TokenString);
    public class UserJwtAuthorizeValidator : AbstractValidator<UserJwtAuthorizeQuery>
    {
        public UserJwtAuthorizeValidator()
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