using FluentValidation;
using LightChat.Web.Models;

namespace LightChat.Web.Validators
{
    public class CreateChatDtoValidator : AbstractValidator<CreateChatDto>
    {
        public CreateChatDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Название чата не может быть пустым")
                .MinimumLength(3).WithMessage("Название чата должно быть не менее 3 символов");
        }
    }
}
