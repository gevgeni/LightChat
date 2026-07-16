using MediatR;

namespace LightChat.Core.Features.Chats
{
    public record GetChatMembersQuery(Guid ChatId) : IRequest<IEnumerable<ChatMembersDto>>;
    public record ChatMembersDto(Guid Id, string Username, string Email, bool IsOnline);
}