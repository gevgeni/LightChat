using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Chats.GetChatMembers;

namespace LightChat.Core.Tests.Handlers.Chats
{
    public class GetChatMembersQueryHandlerTests
    {
        private readonly Mock<IChatRepository> _chatRepositoryMock = new();
        private readonly Mock<IUserStatusManager> _statusManagerMock = new();

        private readonly GetChatMembersHandler _handler;

        public GetChatMembersQueryHandlerTests()
        {
            _handler = new GetChatMembersHandler(
                _chatRepositoryMock.Object, 
                _statusManagerMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserIsNotMember()
        {
            var query = new GetChatMembersQuery(Guid.NewGuid(), Guid.NewGuid());

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(query.ChatId, query.UserId))
                .ReturnsAsync(false);

            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Вы не состоите в этом чате.");

            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.GetMembersAsync(It.IsAny<Guid>()), Times.Never);
            _statusManagerMock.Verify(m => m.IsUserOnline(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnChatMembers()
        {
            var query = new GetChatMembersQuery(Guid.NewGuid(), Guid.NewGuid());
            var members = new List<User>
            {
                new() { Id = Guid.NewGuid(), Username = "username1", PasswordHash = "hashed_pass" },
                new() { Id = Guid.NewGuid(), Username = "username2", PasswordHash = "hashed_pass" },
                new() { Id = Guid.NewGuid(), Username = "username3", PasswordHash = "hashed_pass" },
            };

            var expectedMembers = members.Select(m => new ChatMemberDto(
                m.Id,
                m.Username, 
                m.Email,
                false
            ));

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(query.ChatId, query.UserId))
                .ReturnsAsync(true);

            _chatRepositoryMock
                .Setup(repo => repo.GetMembersAsync(query.ChatId))
                .ReturnsAsync(members);

            _statusManagerMock
                .Setup(m => m.IsUserOnline(It.IsAny<Guid>()))
                .Returns(false);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedMembers);

            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.GetMembersAsync(It.IsAny<Guid>()), Times.Once);
            _statusManagerMock.Verify(m => m.IsUserOnline(It.IsAny<Guid>()), Times.Exactly(members.Count));
        }
    }
}