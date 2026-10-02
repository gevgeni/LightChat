using Moq;

using LightChat.Core.Repositories;
using LightChat.Core.Features.Chats.CreateChat;
using LightChat.Core.Entities;
using FluentAssertions;
using LightChat.Core.Interfaces;

namespace LightChat.Core.Tests.Handlers.Chats
{
    public class CreateDirectChatCommandHandlerTests
    {
        private readonly Mock<IChatRepository> _chatRepositoryMock = new();
        private readonly Mock<ICacheInvalidator> _cacheInvalidator = new();

        private readonly CreateDirectChatHandler _handler;

        public CreateDirectChatCommandHandlerTests()
        {
            _handler = new CreateDirectChatHandler(
                _chatRepositoryMock.Object,
                _cacheInvalidator.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnExistingChat_When_ChatExisted()
        {
            var command = new CreateDirectChatCommand(Guid.NewGuid(), Guid.NewGuid());
            var chat = new Chat() { Id = Guid.NewGuid(), Name = "chat_name", IsDirect = false };

            var existingChat = new ChatResultDto(chat.Id, chat.Name, chat.CreatedAt, chat.IsDirect);

            _chatRepositoryMock
                .Setup(repo => repo.GetDirectChatAsync(command.CreatorUserId, command.TargetUserId))
                .ReturnsAsync(chat);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().Be(existingChat);

            _chatRepositoryMock.Verify(repo => repo.GetDirectChatAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.CreateDirectChatAsync(It.IsAny<Chat>(), It.IsAny<ChatMember>(), It.IsAny<ChatMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnNewChat_When_ChatNotExists()
        {
            var command = new CreateDirectChatCommand(Guid.NewGuid(), Guid.NewGuid());

            _chatRepositoryMock
                .Setup(repo => repo.GetDirectChatAsync(command.CreatorUserId, command.TargetUserId))
                .ReturnsAsync((Chat?)null);

            _chatRepositoryMock
                .Setup(repo => repo.CreateDirectChatAsync(It.IsAny<Chat>(), It.IsAny<ChatMember>(), It.IsAny<ChatMember>()))
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.IsDirect.Should().BeTrue();

            _chatRepositoryMock.Verify(repo => repo.CreateDirectChatAsync(
                It.Is<Chat>(c => c.IsDirect), 
                It.Is<ChatMember>(m => m.UserId == command.CreatorUserId), 
                It.Is<ChatMember>(m => m.UserId == command.TargetUserId)
            ), Times.Once);
        }
    }
}