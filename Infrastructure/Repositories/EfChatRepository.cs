using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LightChat.Infrastructure.Repositories
{
    public class EfChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext _context;

        public EfChatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddMemberAsync(ChatMember member)
        {
            await _context.ChatMembers.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task CreateAsync(Chat chat)
        {
            await _context.Chats.AddAsync(chat);
            await _context.SaveChangesAsync();
        }

        public async Task<Chat?> GetByIdAsync(Guid id)
            => await _context.Chats.FindAsync(id);

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId)
        {
            return await _context.ChatMembers
                .Where(cm => cm.UserId == userId)
                .Select(cm => cm.Chat)
                .ToListAsync();
        }

        public async Task<bool> IsMemberAsync(Guid chatId, Guid userId)
            => await _context.ChatMembers.AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
    }
}