namespace LightChat.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMember> ChatMembers { get; set; } = [];
    }
}