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

        }

    }
}