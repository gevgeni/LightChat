using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Chats.CreateChat;

namespace LightChat.Core.Tests.Handlers
{
    public class CreateChatCommandHandlerTests
    {
        private readonly Mock<IChatRepository> _chatRepositoryMock = new();

        private readonly CreateChatHandler _handler;

        public CreateChatCommandHandlerTests()
        {
            _handler = new CreateChatHandler(_chatRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnChat()
        {
            var command = new CreateChatCommand("chat_name", Guid.NewGuid());
            var chat = new Chat() { Id = Guid.NewGuid(), Name = command.Name, IsDirect = false, CreatedAt = DateTime.UtcNow };
            var chatCreator = new ChatMember() { UserId = Guid.NewGuid(), ChatId = chat.Id};

            _chatRepositoryMock
                .Setup(repo => repo.CreateGroupChatAsync(chat, chatCreator))
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Name.Should().Be(chat.Name);
            result.IsDirect.Should().Be(chat.IsDirect);

            _chatRepositoryMock.Verify(repo => repo.CreateGroupChatAsync(It.IsAny<Chat>(), It.IsAny<ChatMember>()), Times.Once);
        }
    }
}