using LightChat.Core.Interfaces;

namespace LightChat.Core.Features.Users.GetAllUsers
{
    public record GetAllUsersQuery(Guid UserId) : ICacheableQuery<IEnumerable<UserDto>>
    {
        public string CacheKey => "users:all";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    }
    public record UserDto(Guid Id, string Username, bool IsOnline);
}