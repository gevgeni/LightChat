using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LightChat.Infrastructure.Repositories
{
    public class EfMessageRepository : IMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public EfMessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Message>> GetChatHistoryAsync(Guid chatId, int limit, Guid? beforeMessageId = null)
        {
            var query = _context.Messages.Where(m => m.ChatId == chatId);
            if (beforeMessageId.HasValue)
            {
                var before = await _context.Messages
                    .Where(x => x.Id == beforeMessageId)
                    .Select(x => new { x.SentAt, x.Id })
                    .FirstOrDefaultAsync();

                if (before != null)
                {
                    query = query.Where(m => m.SentAt < before.SentAt ||
                        (m.SentAt == before.SentAt && m.Id < before.Id));
                }
            }

            return await query
                .OrderByDescending(m => m.SentAt)
                .ThenByDescending(m => m.Id)
                .Take(limit)
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
                .ToListAsync();
        }

        public async Task MarkUnreadAsReadAsync(Guid chatId, Guid readerId)
        {
            await _context.Messages
                .Where(m => m.ChatId == chatId && m.SenderId != readerId && !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
        }

        public async Task SaveAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
        }
    }
}