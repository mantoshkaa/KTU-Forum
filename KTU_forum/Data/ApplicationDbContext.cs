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

        // just for clarity control, since there are multiple relationships related to replies
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // configure nested reply relationship
            modelBuilder.Entity<ReplyModel>()
                .HasOne(r => r.ParentReply)
                .WithMany(r => r.ChildrenReplies)
                .HasForeignKey(r => r.ParentReplyId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}