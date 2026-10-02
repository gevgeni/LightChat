using MediatR;

namespace LightChat.Core.Features.Chats.LeaveChat
{
    public record LeaveChatCommand(Guid ChatId, Guid UserId) : IRequest;
}