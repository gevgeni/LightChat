namespace LightChat.Core.Entities
{
    public class Message
    {
        public Guid Id { get; set; }

        public Guid ChatId { get; set; }
        public Chat Chat { get; set; } = null!;

        public Guid SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public string Text { get; set; } = null!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
