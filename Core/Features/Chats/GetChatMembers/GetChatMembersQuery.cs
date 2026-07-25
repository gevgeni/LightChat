using MediatR;

namespace LightChat.Core.Features.Chats.GetChatMembers
{
    public record GetChatMembersQuery(Guid ChatId, Guid UserId) : IRequest<IEnumerable<ChatMembersDto>>;
    public record ChatMembersDto(Guid Id, string Username, string Email, bool IsOnline);
}