using LightChat.Core.Entities;

namespace LightChat.Core.Repositories
{
    public interface IPushSubscriptionRepository
    {
        Task AddOrUpdateSubscriptionAsync(UserPushSubscription pushSubscription);
        Task<IEnumerable<Guid>> GetChatRecipientIdsAsync(Guid chatId, Guid senderId);

        Task<IEnumerable<UserPushSubscription>> GetSubscriptionsByUserIdAsync(Guid userId);
        Task RemoveSubscriptionAsync(Guid id);
    }
}