using LightChat.Core.Interfaces;
using StackExchange.Redis;

namespace LightChat.Infrastructure.Services
{
    public class RedisCacheInvalidator : ICacheInvalidator
    {
        private readonly IDatabase _redisDb;

        public RedisCacheInvalidator(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        public async Task InvalidateAllUsersListAsync()
        {
            await _redisDb.KeyDeleteAsync("users:all");
        }

        public async Task InvalidateChatMembersAsync(Guid chatId)
        {
            await _redisDb.KeyDeleteAsync($"chats:{chatId}:members");
        }

        public async Task InvalidateUserByIdAsync(Guid userId)
        {
            await _redisDb.KeyDeleteAsync($"users:{userId}");
        }

        public async Task InvalidateUserChatsAsync(params Guid[] userIds)
        {
            var keys = userIds.Select(id => (RedisKey)$"chats:user:{id}").ToArray();
            await _redisDb.KeyDeleteAsync(keys);
        }
    }
}