using MediatR;

namespace LightChat.Core.Features.Chats
{
    public record GetUserChatsQuery(Guid UserId) : IRequest<IEnumerable<ChatResultDto>>;
}
