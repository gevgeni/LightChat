using LightChat.Core.Features.Chats.CreateChat;
using LightChat.Core.Interfaces;

namespace LightChat.Core.Features.Chats.GetUserChats
{
    public record GetUserChatsQuery(Guid UserId) : ICacheableQuery<IEnumerable<ChatResultDto>>
    {
        public string CacheKey => $"chats:user:{UserId}";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
    }
}