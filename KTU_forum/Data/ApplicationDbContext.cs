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
        }

    }
}