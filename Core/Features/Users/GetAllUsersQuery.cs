using MediatR;

namespace LightChat.Core.Features.Users
{
    public record GetAllUsersQuery(Guid UserId) : IRequest<IEnumerable<UserDto>>;
    public record UserDto(Guid Id, string Username, bool IsOnline);
}
