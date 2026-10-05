using MediatR;
using System.Text.Json;
using StackExchange.Redis;
using LightChat.Core.Interfaces;

namespace LightChat.Web.Middlwares
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICacheableQuery<TResponse>
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CachingBehavior(
            IConnectionMultiplexer redis,
            ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
            _redisDb = redis.GetDatabase();
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            string cacheKey = request.CacheKey;
            try
            {
                RedisValue cachedData = await _redisDb.StringGetAsync(cacheKey);
                if (cachedData.HasValue)
                {
                    var deserialized = JsonSerializer.Deserialize<TResponse>(cachedData!, JsonOptions);
                    if (deserialized is not null)
                        return deserialized;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка чтения из кэша для ключа {Key}", cacheKey);
            }

            TResponse response = await next(cancellationToken);

            try
            {
                if (response is not null)
                {
                    string serializedData = JsonSerializer.Serialize(response, JsonOptions);
                    TimeSpan expiry = request.Expiration ?? TimeSpan.FromMinutes(5);

                    await _redisDb.StringSetAsync(cacheKey, serializedData, expiry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка записи в кэш для ключа {Key}", cacheKey);
            }
            return response;
        }
    }
}