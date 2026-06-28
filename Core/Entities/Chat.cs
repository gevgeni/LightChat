namespace LightChat.Core.Entities
{
    public class Chat
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Message> Messages { get; set; } = [];
        public ICollection<ChatMember> ChatMembers { get; set; } = [];
    }
}
