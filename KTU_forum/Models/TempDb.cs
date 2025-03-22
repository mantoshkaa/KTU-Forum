using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

namespace KTU_forum.Models
{
    public class TempDb : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("TempDb");
        }

        // Constructor that accepts DbContextOptions
        public TempDb(DbContextOptions<TempDb> options) : base(options)
        {
        }
    }
}
