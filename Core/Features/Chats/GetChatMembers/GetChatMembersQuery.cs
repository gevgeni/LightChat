using LightChat.Core.Interfaces;

namespace LightChat.Core.Features.Chats.GetChatMembers
{
    public record GetChatMembersQuery(Guid ChatId, Guid UserId) : ICacheableQuery<IEnumerable<ChatMemberDto>>
    {
        public string CacheKey => $"chats:{ChatId}:members";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(15);
    }
    public record ChatMemberDto(Guid Id, string Username, string Email, bool IsOnline);
}