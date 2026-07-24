using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Users.UserJwtAuthorize;

namespace LightChat.Core.Tests.Handlers
{
    public class UserJwtAuthorizeQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();

        private readonly UserJwtAuthorizeHandler _handler;

        public UserJwtAuthorizeQueryHandlerTests()
        {
            _handler = new UserJwtAuthorizeHandler(
                _passwordHasherMock.Object,
                _userRepositoryMock.Object,
                _jwtTokenGeneratorMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ThrowUnauthorizedException_When_UserDoesNotExists()
        {
            var query = new UserJwtAuthorizeQuery("nonexistent_user", "password123!");

            _userRepositoryMock
                .Setup(repo => repo.GetByUsernameAsync(query.Username))
                .ReturnsAsync((User?)null);

            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Неверное имя пользователя или пароль.");

            _passwordHasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _jwtTokenGeneratorMock.Verify(g => g.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ThrowUnauthorizedException_When_PasswordIsInvalid()
        {
            var query = new UserJwtAuthorizeQuery("valid_user", "wrong_password");
            var user = new User { Id = Guid.NewGuid(), Username = "valid_user", PasswordHash = "hashed_pass" };

            _userRepositoryMock
                .Setup(repo => repo.GetByUsernameAsync(query.Username))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.VerifyPassword(query.Password, user.PasswordHash))
                .Returns(false);

            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Неверное имя пользователя или пароль.");

            _jwtTokenGeneratorMock.Verify(g => g.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnToken_When_CredentialsAreValid()
        {
            var query = new UserJwtAuthorizeQuery("valid_user", "correct_password");
            var user = new User { Id = Guid.NewGuid(), Username = "valid_user", PasswordHash = "hashed_pass" };
            var expectedToken = "mocked.jwt.token";

            _userRepositoryMock
                .Setup(r => r.GetByUsernameAsync(query.Username))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.VerifyPassword(query.Password, user.PasswordHash))
                .Returns(true);

            _jwtTokenGeneratorMock
                .Setup(g => g.GenerateToken(user.Id, user.Username))
                .Returns(expectedToken);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.TokenString.Should().Be(expectedToken);
            _passwordHasherMock.Verify(h => h.VerifyPassword(query.Password, user.PasswordHash), Times.Once);
        }
    }
}