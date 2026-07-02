using Microsoft.EntityFrameworkCore;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Infrastructure.Persistence;

namespace LightChat.Infrastructure.Repositories
{
    public class EfUserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public EfUserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
            => await _context.Users.AnyAsync(u => u.Id == id);

        public async Task<List<User>> GetAllAsync()
            => await _context.Users.ToListAsync();

        public async Task<List<User>> GetAllContainsInIdsAsync(List<Guid> ids)
            => await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();

        public async Task<User?> GetByIdAsync(Guid id)
            => await _context.Users.FindAsync(id);

        public async Task<User?> GetByUsernameAsync(string username)
            => await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }
}