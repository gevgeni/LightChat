using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LightChat.Infrastructure.Repositories
{
    public class UserPushSubscriptionRepository : IPushSubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public UserPushSubscriptionRepository(ApplicationDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task AddOrUpdateSubscriptionAsync(UserPushSubscription pushSubscription)
        {
            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == pushSubscription.Endpoint);

            if (existing != null)
            {
                existing.UserId = pushSubscription.UserId;
                existing.P256dh = pushSubscription.P256dh;
                existing.Auth = pushSubscription.Auth;
                existing.CreatedAt = pushSubscription.CreatedAt;
            }
            else
            {
                await _context.PushSubscriptions.AddAsync(pushSubscription);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Guid>> GetChatRecipientIdsAsync(Guid chatId, Guid senderId)
        {
            return await _context.ChatMembers
                .Where(cm => cm.ChatId == chatId && cm.UserId != senderId)
                .Select(cm => cm.UserId)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserPushSubscription>> GetSubscriptionsByUserIdAsync(Guid userId)
        {
            return await _context.PushSubscriptions
                .AsNoTracking()
                .Where(cm => cm.UserId == userId)
                .ToListAsync();
        }

        public async Task RemoveSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription != null)
            {
                _context.PushSubscriptions.Remove(subscription);
                await _context.SaveChangesAsync();
            }
        }
    }
}
