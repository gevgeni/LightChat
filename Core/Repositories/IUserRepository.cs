using LightChat.Core.Entities;

namespace LightChat.Core.Repositories
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<List<User>> GetAllContainsInIdsAsync(List<Guid> ids);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameAsync(string username);
        Task CreateAsync(User user);
        Task<bool> ExistsAsync(Guid id);
    }
}