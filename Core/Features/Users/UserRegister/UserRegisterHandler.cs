using MediatR;

using LightChat.Core.Entities;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Users.GetAllUsers;

namespace LightChat.Core.Features.Users.UserRegister
{
    public class UserRegisterHandler : IRequestHandler<UserRegisterCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        public UserRegisterHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserDto> Handle(UserRegisterCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
                throw new InvalidOperationException("Пользователь с таким ником уже существует.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);

            return new UserDto(user.Id, user.Username, false);
        }
    }
}