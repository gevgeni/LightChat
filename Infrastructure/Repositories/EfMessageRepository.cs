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
            DateTime? beforeMessageTargetDate = null;

            if (beforeMessageId.HasValue)
            {
                beforeMessageTargetDate = await _context.Messages
                    .Where(x => x.Id == beforeMessageId)
                    .Select(x => x.SentAt)
                    .FirstOrDefaultAsync();
            }

            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .Where(m => beforeMessageTargetDate == null || m.SentAt < beforeMessageTargetDate)
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .Reverse()
                .ToListAsync();
        }

        public async Task SaveAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
        }
    }
}