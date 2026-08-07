using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using StackExchange.Redis;
using System.Text.Json;

namespace LightChat.Infrastructure.Repositories
{
    public class CachedUserRepository : IUserRepository
    {
        private readonly IUserRepository _innerRepository;
        private readonly IDatabase _redisDb;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        public CachedUserRepository(IUserRepository innerRepository, IConnectionMultiplexer redis)
        {
            _innerRepository = innerRepository;
            _redisDb = redis.GetDatabase();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            try
            {
                string cacheKey = $"users:{id}";

                RedisValue cachedUser = await _redisDb.StringGetAsync(cacheKey);
                if (cachedUser.HasValue)
                    return JsonSerializer.Deserialize<User>(cachedUser!);

                User? user = await _innerRepository.GetByIdAsync(id);
                if (user is not null)
                {
                    await _redisDb.StringSetAsync(
                        cacheKey,
                        JsonSerializer.Serialize(user),
                        CacheExpiration);
                }

                return user;
            }
            catch (Exception)
            {
                return await _innerRepository.GetByIdAsync(id);
            }
        }

        public Task CreateAsync(User user) => _innerRepository.CreateAsync(user);
        public Task<bool> ExistsAsync(Guid id) => _innerRepository.ExistsAsync(id);
        public Task<List<User>> GetAllAsync() => _innerRepository.GetAllAsync();
        public Task<List<User>> GetAllContainsInIdsAsync(List<Guid> ids) => _innerRepository.GetAllContainsInIdsAsync(ids);
        public Task<User?> GetByUsernameAsync(string username) => _innerRepository.GetByUsernameAsync(username);
    }
}
