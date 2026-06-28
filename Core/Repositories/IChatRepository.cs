using LightChat.Core.Entities;

namespace LightChat.Core.Repositories
{
    public interface IChatRepository
    {
        Task<Chat?> GetByIdAsync(Guid id);
        Task CreateAsync(Chat chat);

        Task AddMemberAsync(ChatMember member);
        Task<bool> IsExistsAsync(Guid chatId, Guid userId);

        Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId);
    }
}