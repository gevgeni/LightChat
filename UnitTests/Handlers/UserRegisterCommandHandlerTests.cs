using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Users.UserRegister;

namespace LightChat.Core.Tests.Handlers
{
    public class UserRegisterCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

        private readonly UserRegisterHandler _handler;

        public UserRegisterCommandHandlerTests()
        {
            _handler = new UserRegisterHandler(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ThrowInvalidOperationException_When_UserExists()
        {
            var command = new UserRegisterCommand("existent_user", "email@email.com", "password123");
            var user = new User { Id = Guid.NewGuid(), Username = "existent_user", PasswordHash = "hashed_pass" };

            _userRepositoryMock
                .Setup(repo => repo.GetByUsernameAsync(command.Username))
                .ReturnsAsync(user);

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Пользователь с таким ником уже существует.");

            _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnUser_When_CredentialsAreValid()
        {
            var command = new UserRegisterCommand("valid_user", "email@email.com", "password123");
            string hashedPass = "mocked_hash";

            _userRepositoryMock
                .Setup(repo => repo.GetByUsernameAsync(command.Username))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(h => h.HashPassword(command.Password))
                .Returns(hashedPass);

            User? capturedUser = null;

            _userRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
                .Callback<User>(user => capturedUser = user)
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();

            capturedUser.Should().NotBeNull();
            result.Id.Should().Be(capturedUser!.Id);
            result.Username.Should().Be(capturedUser!.Username);
            result.IsOnline.Should().BeFalse();

            _passwordHasherMock.Verify(h => h.HashPassword(command.Password), Times.Once);
        }
    }
}