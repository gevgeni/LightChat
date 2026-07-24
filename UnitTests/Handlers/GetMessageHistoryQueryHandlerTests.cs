using Moq;
using FluentAssertions;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Core.Features.Messages.GetMessageHistory;

namespace LightChat.Core.Tests.Handlers
{
    public class GetMessageHistoryQueryHandlerTests
    {
        private readonly Mock<IChatRepository> _chatRepositoryMock = new();
        private readonly Mock<IMessageRepository> _messageRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();

        private readonly GetMessageHistoryHandler _handler;

        public GetMessageHistoryQueryHandlerTests()
        {
            _handler = new GetMessageHistoryHandler(
                _chatRepositoryMock.Object,
                _messageRepositoryMock.Object,
                _userRepositoryMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserIsNotChatMember()
        {
            var query = new GetMessageHistoryQuery(Guid.NewGuid(), Guid.NewGuid(), 10, null);

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(query.ChatId, query.UserId))
                .ReturnsAsync(false);

            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Вы не состоите в этом чате.");

            _messageRepositoryMock.Verify(repo => repo.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
            _userRepositoryMock.Verify(repo => repo.GetAllContainsInIdsAsync(It.IsAny<List<Guid>>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnMessages_When_UserIsChatMember()
        {
            var query = new GetMessageHistoryQuery(Guid.NewGuid(), Guid.NewGuid(), 5, null);
            var senders = new List<User>
            {
                new() { Id = Guid.NewGuid(), Username = "user1", PasswordHash = "hashed_pass"},
                new() { Id = Guid.NewGuid(), Username = "user2", PasswordHash = "hashed_pass"},
                new() { Id = Guid.NewGuid(), Username = "user3", PasswordHash = "hashed_pass"}
            };
            var messagesHistory = new List<Message>
            { 
                new() { Id = Guid.NewGuid(), ChatId = query.ChatId, SenderId = senders[0].Id, Text = "message1"},
                new() { Id = Guid.NewGuid(), ChatId = query.ChatId, SenderId = senders[2].Id, Text = "message2"},
                new() { Id = Guid.NewGuid(), ChatId = query.ChatId, SenderId = senders[0].Id, Text = "message3"},
                new() { Id = Guid.NewGuid(), ChatId = query.ChatId, SenderId = senders[1].Id, Text = "message4"},
                new() { Id = Guid.NewGuid(), ChatId = query.ChatId, SenderId = senders[0].Id, Text = "message5"},
            };
            

            var expectedMessages = messagesHistory.Select(m => new MessageDto
            (
                m.Id,
                m.ChatId,
                m.SenderId,
                senders.First(s => s.Id == m.SenderId).Username,
                m.Text,
                m.SentAt,
                m.IsRead
            ));

            _chatRepositoryMock
                .Setup(repo => repo.IsMemberAsync(query.ChatId, query.UserId))
                .ReturnsAsync(true);

            _messageRepositoryMock
                .Setup(repo => repo.GetChatHistoryAsync(query.ChatId, query.Limit, query.BeforeMessageId))
                .ReturnsAsync(messagesHistory);

            _userRepositoryMock
                .Setup(repo => repo.GetAllContainsInIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(senders);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedMessages);

            _chatRepositoryMock.Verify(repo => repo.IsMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
            _messageRepositoryMock.Verify(repo => repo.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), null), Times.Once);
            _userRepositoryMock.Verify(repo => repo.GetAllContainsInIdsAsync(It.IsAny<List<Guid>>()), Times.Once);
        }
    }
}
