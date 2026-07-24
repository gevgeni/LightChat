using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Chats.CreateChat;
using LightChat.Core.Features.Chats.GetUserChats;

namespace LightChat.Core.Tests.Handlers
{
    public class GetUserChatsQueryHandlerTests
    {
        private readonly Mock<IChatRepository> _chatRepositoryMock = new();

        private readonly GetUserChatsHandler _handler;

        public GetUserChatsQueryHandlerTests()
        {
            _handler = new GetUserChatsHandler(_chatRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnUserChats()
        {
            var query = new GetUserChatsQuery(Guid.NewGuid());
            var chats = new List<Chat>
            {
                new() { Id = Guid.NewGuid(), Name = "chat1", IsDirect = false },
                new() { Id = Guid.NewGuid(), Name = "chat2", IsDirect = false },
                new() { Id = Guid.NewGuid(), Name = "chat3", IsDirect = false },
            };

            var expectedChats = chats.Select(c => new ChatResultDto(
                c.Id,
                c.Name,
                c.CreatedAt,
                c.IsDirect
            ));

            _chatRepositoryMock
                .Setup(repo => repo.GetUserChatsAsync(query.UserId))
                .ReturnsAsync(chats);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedChats);

            _chatRepositoryMock.Verify(repo => repo.GetUserChatsAsync(It.IsAny<Guid>()), Times.Once);
        }
    }
}
