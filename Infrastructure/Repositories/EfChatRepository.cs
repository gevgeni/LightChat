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

        public async Task CreateGroupChatAsync(Chat chat, ChatMember member)
        {
            _context.Chats.Add(chat);
            _context.ChatMembers.Add(member);
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

        public async Task<IEnumerable<User>> GetMembersAsync(Guid chatId)
        {
            return await _context.ChatMembers
                .Where(cm => cm.ChatId == chatId)
                .Select(cm => cm.User)
                .ToListAsync();
        }

        public async Task<bool> IsMemberAsync(Guid chatId, Guid userId)
            => await _context.ChatMembers.AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);

        public async Task<Chat?> GetDirectChatAsync(Guid currentUserId, Guid targetUserId)
        {
            return await _context.Chats
                .Where(c => c.IsDirect)
                .FirstOrDefaultAsync(c =>
                    c.ChatMembers.Any(cm => cm.UserId == currentUserId) &&
                    c.ChatMembers.Any(cm => cm.UserId == targetUserId));
        }

        public async Task CreateDirectChatAsync(Chat chat, ChatMember currentUser, ChatMember targetUser)
        {
            _context.Chats.Add(chat);
            _context.ChatMembers.Add(currentUser);
            _context.ChatMembers.Add(targetUser);
            await _context.SaveChangesAsync();
        }
    }
}