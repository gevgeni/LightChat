using LightChat.Core.Entities;

namespace LightChat.Core.Repositories
{
    public interface IMessageRepository
    {
        Task SaveAsync(Message message);
        Task MarkUnreadAsReadAsync(Guid chatId, Guid readerId);
        Task<IEnumerable<Message>> GetChatHistoryAsync(Guid chatId, int limit, Guid? beforeMessageId = null);
    }
}