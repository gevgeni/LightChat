using MediatR;

namespace LightChat.Core.Interfaces
{
    public interface ICacheableQuery<TResponse> : IRequest<TResponse>
    {
        string CacheKey { get; }
        TimeSpan? Expiration { get; }
    }
}