using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Chats.AddChatMember;

namespace LightChat.Core.Tests.Handlers
{
    public class AddChatMemberCommandHandlerTests
    {
        private readonly Mock<IChatRepository> _chatRepositoryMock = new();

        private readonly AddChatMemberHandler _handler;

        public AddChatMemberCommandHandlerTests()
        {
            _handler = new AddChatMemberHandler(_chatRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ThrowKeyNotFoundException_When_ChatDoesNotExist()
        {
            var command = new AddChatMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            _chatRepositoryMock
                .Setup(repo => repo.GetByIdAsync(command.ChatId))
                .ReturnsAsync((Chat?)null);

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Чат не найден.");

            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            _chatRepositoryMock.Verify(repo => repo.AddMemberAsync(It.IsAny<ChatMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ThrowInvalidOperationException_When_ChatIsDirect()
        {
            var command = new AddChatMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var chat = new Chat() { Id = command.ChatId, Name = "chat_name", IsDirect = true };

            _chatRepositoryMock
                .Setup(repo => repo.GetByIdAsync(command.ChatId))
                .ReturnsAsync(chat);

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("В личный чат нельзя приглашать сторонних участников.");

            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            _chatRepositoryMock.Verify(repo => repo.AddMemberAsync(It.IsAny<ChatMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserIsNotChatMember()
        {
            var command = new AddChatMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var chat = new Chat() { Id = command.ChatId, Name = "chat_name" };

            _chatRepositoryMock
                .Setup(repo => repo.GetByIdAsync(command.ChatId))
                .ReturnsAsync(chat);

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(command.ChatId, command.CurrentUserId))
                .ReturnsAsync(false);

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("У вас нет доступа для добавления участников в этот чат.");

            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), command.CurrentUserId), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), command.TargetUserId), Times.Never);
            _chatRepositoryMock.Verify(repo => repo.AddMemberAsync(It.IsAny<ChatMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ThrowInvalidOperationException_When_TargetUserIsAlreadyMember()
        {
            var command = new AddChatMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var chat = new Chat() { Id = command.ChatId, Name = "chat_name" };

            _chatRepositoryMock
                .Setup(repo => repo.GetByIdAsync(command.ChatId))
                .ReturnsAsync(chat);

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(command.ChatId, command.CurrentUserId))
                .ReturnsAsync(true);

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(command.ChatId, command.TargetUserId))
                .ReturnsAsync(true);

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Пользователь уже состоит в этом чате.");

            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), command.CurrentUserId), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), command.TargetUserId), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.AddMemberAsync(It.IsAny<ChatMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnMember_When_RequestIsValid()
        {
            var command = new AddChatMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var chat = new Chat() { Id = command.ChatId, Name = "chat_name" };
            var chatMember = new ChatMember() { UserId = command.TargetUserId, ChatId = chat.Id };

            _chatRepositoryMock
                .Setup(repo => repo.GetByIdAsync(command.ChatId))
                .ReturnsAsync(chat);

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(command.ChatId, command.CurrentUserId))
                .ReturnsAsync(true);

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(command.ChatId, command.TargetUserId))
                .ReturnsAsync(false);

            _chatRepositoryMock
                .Setup(repo => repo.AddMemberAsync(chatMember))
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.TargetUserId.Should().Be(command.TargetUserId);
            result.ChatId.Should().Be(command.ChatId);
            result.ChatName.Should().Be(chat.Name);
            result.IsDirect.Should().Be(chat.IsDirect);

            _chatRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), command.CurrentUserId), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), command.TargetUserId), Times.Once);
            _chatRepositoryMock.Verify(repo => repo.AddMemberAsync(It.IsAny<ChatMember>()), Times.Once);
        }
    }
}