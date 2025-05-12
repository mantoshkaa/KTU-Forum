// Create a background service in Services/RoleUpdateService.cs
using KTU_forum.Data;
using KTU_forum.Services;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq; // This is needed for LINQ extension methods like Select
using Microsoft.EntityFrameworkCore; // This is needed for EF Core extension methods

namespace KTU_forum.Services
{
    public class RoleUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public RoleUpdateService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var roleService = scope.ServiceProvider.GetRequiredService<RoleService>();

                    // Get all users who might need role updates
                    var users = await context.Users.Select(u => u.Id).ToListAsync();

                    foreach (var userId in users)
                    {
                        await roleService.UpdateUserRoles(userId);
                    }
                }

                // Run once a day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }

}

