using Microsoft.EntityFrameworkCore;
using ChattyMcChatface.Data.Entities;

namespace ChattyMcChatface.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Chatroom> Chatrooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<LastRead> LastReads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the composite primary key for LastRead entity
            modelBuilder.Entity<LastRead>().HasKey(lr => new { lr.UserId, lr.ChatroomId });

            // Configure one-to-many relationships
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(cm => cm.UserId);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Chatroom)
                .WithMany(cr => cr.Chats)
                .HasForeignKey(cm => cm.ChatroomId);

            // Configure LastRead relationships
            modelBuilder.Entity<LastRead>()
                .HasOne(lr => lr.User)
                .WithMany(u => u.LastReads)
                .HasForeignKey(lr => lr.UserId);

            modelBuilder.Entity<LastRead>()
                .HasOne(lr => lr.Chatroom)
                .WithMany(cr => cr.LastReads)
                .HasForeignKey(lr => lr.ChatroomId);

            // The many-to-many relationship between User and Chatroom
            // is handled automatically by EF Core 5+ through the navigation properties
        }
    }
}