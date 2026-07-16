using FluentValidation;
using MediatR;

namespace LightChat.Core.Features.Chats
{
    public record CreateChatCommand(string Name, Guid CreatorUserId) : IRequest<ChatResultDto>;
    public record CreateDirectChatCommand(Guid CreatorUserId, Guid TargetUserId) : IRequest<ChatResultDto>;
    public record ChatResultDto(Guid Id, string Name, DateTime CreatedAt, bool IsDirect);
}