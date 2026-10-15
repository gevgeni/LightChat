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
            await DeleteByPatternAsync("users:all");
        }

        public async Task InvalidateChatMembersAsync(Guid chatId)
        {
            await DeleteByPatternAsync($"chats:{chatId}:members");
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

        private async Task DeleteByPatternAsync(string pattern)
        {
            var endpoints = _redisDb.Multiplexer.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redisDb.Multiplexer.GetServer(endpoint);
                if (!server.IsConnected) continue;

                var keys = server.Keys(pattern: pattern, pageSize: 250).ToArray();
                if (keys.Length > 0)
                    await _redisDb.KeyDeleteAsync(keys);
            }
        }
    }
}