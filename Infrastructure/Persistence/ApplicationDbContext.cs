using LightChat.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LightChat.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Chat> Chats { get; set; } = null!;
        public DbSet<ChatMember> ChatMembers { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChatMember>(entity =>
            {
                entity.HasKey(cm => new { cm.ChatId, cm.UserId });

                entity.HasOne(cm => cm.Chat)
                    .WithMany(cm => cm.ChatMembers)
                    .HasForeignKey(cm => cm.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cm => cm.User)
                    .WithMany(u => u.ChatMembers)
                    .HasForeignKey(cm => cm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Text)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.HasOne(m => m.Chat)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => new { m.ChatId, m.SentAt });
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                modelBuilder.Entity<User>()
                    .Property(u => u.PasswordHash)
                    .IsRequired();

                entity.HasIndex(u => u.Username)
                    .IsUnique();

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(100);
            });
        }
    }
}