using Microsoft.EntityFrameworkCore;
using KTU_forum.Models;

namespace KTU_forum.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<PostModel> Posts { get; set; }
        public DbSet<ReplyModel> Replies { get; set; }
        public DbSet<MessageModel> Messages { get; set; }
        public DbSet<RoomModel> Rooms { get; set; }
        public DbSet<LikeModel> Likes { get; set; }
        public DbSet<PrivateMessageModel> PrivateMessages { get; set; }
        public DbSet<ConversationModel> Conversations { get; set; }

        // just for clarity control, since there are multiple relationships related to replies and messages
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // configure nested reply relationship
            modelBuilder.Entity<ReplyModel>()
                .HasOne(r => r.ParentReply)
                .WithMany(r => r.ChildrenReplies)
                .HasForeignKey(r => r.ParentReplyId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<MessageModel>()
                .HasOne(m => m.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageModel>()
                .HasOne(m => m.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserModel>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Define the composite key for the LikeModel
            modelBuilder.Entity<LikeModel>()
                .HasKey(l => new { l.MessageId, l.UserId });  // Composite key: MessageId + UserId

            // Set up the relationship between Likes and Messages
            modelBuilder.Entity<LikeModel>()
                .HasOne(l => l.Message)
                .WithMany(m => m.Likes)  // A message can have many likes
                .HasForeignKey(l => l.MessageId);

            // Set up the relationship between Likes and Users
            modelBuilder.Entity<LikeModel>()
                .HasOne(l => l.User)
                .WithMany()  // No need for a navigation property on UserModel
                .HasForeignKey(l => l.UserId);

            // Configure PrivateMessage relationships
            modelBuilder.Entity<PrivateMessageModel>()
                .HasOne(pm => pm.Sender)
                .WithMany()
                .HasForeignKey(pm => pm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrivateMessageModel>()
                .HasOne(pm => pm.Receiver)
                .WithMany()
                .HasForeignKey(pm => pm.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrivateMessageModel>()
                .HasOne(pm => pm.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(pm => pm.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure self-referencing reply relationship
            modelBuilder.Entity<PrivateMessageModel>()
                .HasOne(pm => pm.ReplyTo)
                .WithMany()
                .HasForeignKey(pm => pm.ReplyToId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ConversationModel relationships
            modelBuilder.Entity<ConversationModel>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConversationModel>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}