using System.Text.Json;

using MediatR;
using StackExchange.Redis;

using LightChat.Core.Interfaces;

namespace LightChat.Web.Middlwares
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICacheableQuery<TResponse>
    {
        private readonly IDatabase _redisDb;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CachingBehavior(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                string cacheKey = request.CacheKey;

                RedisValue cachedData = await _redisDb.StringGetAsync(cacheKey);
                if (cachedData.HasValue)
                {
                    var deserialized = JsonSerializer.Deserialize<TResponse>(cachedData!, JsonOptions);
                    if (deserialized is not null)
                        return deserialized;
                }

                TResponse response = await next(cancellationToken);
                if (response is not null)
                {
                    string serializedData = JsonSerializer.Serialize(response, JsonOptions);
                    TimeSpan expiry = request.Expiration ?? TimeSpan.FromMinutes(5);

                    await _redisDb.StringSetAsync(cacheKey, serializedData, expiry);
                }

                return response;
            }
            catch (Exception)
            {
                return await next(cancellationToken);
            }
            
        }
    }
}