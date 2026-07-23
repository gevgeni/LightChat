using MediatR;

namespace LightChat.Core.Features.Messages.GetMessageHistory
{
    public record GetMessageHistoryQuery(Guid ChatId, Guid UserId, int Limit, Guid? BeforeMessageId) : IRequest<IEnumerable<MessageDto>>;
    public record MessageDto(Guid Id, Guid ChatId, Guid SenderId, string SenderUsername, string Text, DateTime SentAt, bool IsRead);
}