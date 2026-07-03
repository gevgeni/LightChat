using LightChat.Core.Entities;

namespace LightChat.Core.Repositories
{
    public interface IChatRepository
    {
        Task<Chat?> GetByIdAsync(Guid id);

        Task AddMemberAsync(ChatMember member);
        Task<bool> IsMemberAsync(Guid chatId, Guid userId);

        Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId);
        Task<IEnumerable<User>> GetMembersAsync(Guid chatId);
        Task CreateGroupChatAsync(Chat chat, ChatMember member);

        Task<Chat?> GetDirectChatAsync(Guid currentUserId, Guid targetUserId);
        Task CreateDirectChatAsync(Chat chat, ChatMember currentUser, ChatMember targetUser);
    }
}