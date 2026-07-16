using MediatR;

using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Users;

namespace LightChat.Infrastructure.Features.Users
{
    internal class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserStatusManager _statusManager;
        public GetAllUsersHandler(IUserRepository userRepository, IUserStatusManager statusManager)
        {
            _userRepository = userRepository;
            _statusManager = statusManager;
        }

        public async Task<IEnumerable<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var allUsers = await _userRepository.GetAllAsync();

            var result = allUsers
                .Where(u => u.Id != request.UserId)
                .Select(u => new UserDto
                (
                    u.Id,
                    u.Username,
                     _statusManager.IsUserOnline(u.Id)
                ));
            return result;
        }
    }
}