using MediatR;
using LightChat.Core.Features.Chats.CreateChat;

namespace LightChat.Core.Features.Chats.GetUserChats
{
    public record GetUserChatsQuery(Guid UserId) : IRequest<IEnumerable<ChatResultDto>>;
}
