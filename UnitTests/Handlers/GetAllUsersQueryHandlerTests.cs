using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Users.GetAllUsers;

namespace LightChat.Core.Tests.Handlers
{
    public class GetAllUsersQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUserStatusManager> _statusManagerMock = new();

        private readonly GetAllUsersHandler _handler;

        public GetAllUsersQueryHandlerTests()
        {
            _handler = new GetAllUsersHandler(
                _userRepositoryMock.Object,
                _statusManagerMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnAllUsers()
        {
            var query = new GetAllUsersQuery(Guid.NewGuid());
            var users = new List<User>
            {
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() }
            };

            var expectedUsers = users
                .Select(u => new UserDto
                (
                    u.Id,
                    u.Username,
                     false
                ));

            _userRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(users);

            _statusManagerMock
                .Setup(m => m.IsUserOnline(It.IsAny<Guid>()))
                .Returns(false);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedUsers);
        }

        [Fact]
        public async Task Handle_Should_NotReturnCurrentUser()
        {
            var query = new GetAllUsersQuery(Guid.NewGuid());
            var users = new List<User>
            {
                new() { Id = query.UserId, Username = "current_user" },
                new() { Id = Guid.NewGuid(), Username = "user1" },
                new() { Id = Guid.NewGuid(), Username = "user2" },
                new() { Id = Guid.NewGuid(), Username = "user3" }
            };

            _userRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(users);

            _statusManagerMock
                .Setup(m => m.IsUserOnline(It.IsAny<Guid>()))
                .Returns(false);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.First().Id.Should().NotBe(query.UserId);
            result.Count().Should().Be(users.Count - 1);

            _userRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
            _statusManagerMock.Verify(m => m.IsUserOnline(It.IsAny<Guid>()), Times.Exactly(users.Count));
        }
    }
}
