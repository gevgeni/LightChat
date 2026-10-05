namespace LightChat.Core.Events
{
    public record MessageSentEvent(
        Guid MessageId,
        Guid ChatId,
        Guid SenderId,
        string SenderUsername,
        string Text,
        DateTime SentAt
    );
}