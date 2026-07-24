using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Chats.GetChatMembers;

namespace LightChat.Core.Tests.Handlers
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
        public async Task Handle_Should_ReturnChatMembers()
        {
            var query = new GetChatMembersQuery(Guid.NewGuid());
            var members = new List<User>
            {
                new() { Id = Guid.NewGuid(), Username = "username1", PasswordHash = "hashed_pass" },
                new() { Id = Guid.NewGuid(), Username = "username2", PasswordHash = "hashed_pass" },
                new() { Id = Guid.NewGuid(), Username = "username3", PasswordHash = "hashed_pass" },
            };

            var expectedMembers = members.Select(m => new ChatMembersDto(
                m.Id,
                m.Username, 
                m.Email,
                false
            ));

            _chatRepositoryMock
                .Setup(repo => repo.GetMembersAsync(query.ChatId))
                .ReturnsAsync(members);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedMembers);
        }
    }
}