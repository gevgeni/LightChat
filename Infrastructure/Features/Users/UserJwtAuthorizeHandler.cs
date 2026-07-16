using MediatR;

using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Users;
using LightChat.Infrastructure.Security;

namespace LightChat.Infrastructure.Features.Users
{
    public class UserJwtAuthorizeHandler : IRequestHandler<UserJwtAuthorizeQuery, JwtTokenDto>
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public UserJwtAuthorizeHandler(
            IPasswordHasher passwordHasher,
            IUserRepository userRepository,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<JwtTokenDto> Handle(UserJwtAuthorizeQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);

            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Неверное имя пользователя или пароль.");

            return new (_jwtTokenGenerator.GenerateToken(user.Id, request.Username));
        }
    }
}