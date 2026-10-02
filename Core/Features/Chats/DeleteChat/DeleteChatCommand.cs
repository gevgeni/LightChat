using MediatR;

namespace LightChat.Core.Features.Chats.DeleteChat
{
    public record DeleteChatCommand(Guid ChatId, Guid UserId) : IRequest;
}