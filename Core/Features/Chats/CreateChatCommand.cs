using FluentValidation;
using MediatR;

namespace LightChat.Core.Features.Chats
{
    public record CreateChatCommand(string Name, Guid CreatorUserId) : IRequest<ChatResultDto>;
    public record CreateDirectChatCommand(Guid CreatorUserId, Guid TargetUserId) : IRequest<ChatResultDto>;
    public record ChatResultDto(Guid Id, string Name, DateTime CreatedAt, bool IsDirect);
    public class CreateChatValidator : AbstractValidator<CreateChatCommand>
    {
        public CreateChatValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Название чата не может быть пустым")
                .MinimumLength(3).WithMessage("Название чата должно быть не менее 3 символов");
            RuleFor(x => x.CreatorUserId)
                .NotEmpty();
        }
    }
}