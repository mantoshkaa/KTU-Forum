using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KTU_forum.Data;
using KTU_forum.Models;
using System;
using System.Threading.Tasks;

namespace KTU_forum
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql("DefaultConnection"));

            services.AddRazorPages();



            // Configure SignalR with user identification

            services.AddSignalR(options => {
                options.EnableDetailedErrors = true; // Enable detailed errors for debugging
                options.MaximumReceiveMessageSize = 102400; // Optional: increase message size limit if needed
            });



            // Add the custom user ID provider for SignalR

            services.AddSingleton<IUserIdProvider, NameUserIdProvider>();



            services.AddControllersWithViews();

            services.AddRazorPages(options =>
            {
                // This ensures Razor Pages with route parameters work correctly
                options.Conventions.AddPageRoute("/PublicProfile", "/Profile/{username}");
            });

            services.AddHttpContextAccessor();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);  // Session timeout after 30 minutes
                options.Cookie.HttpOnly = true;  // Helps prevent XSS attacks
                options.Cookie.IsEssential = true;  // Required for non-logged-in users
            });



            services.AddSingleton<KTU_forum.Services.OnlineUserService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();



            // Important: Session middleware must be called before SignalR endpoints

            app.UseSession();



            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<Hubs.ChatHub>("/chatHub");
                endpoints.MapControllers(); // Map controllers for API endpoints
                endpoints.MapGet("/KeepAlive", async context =>
                {
                    context.Session.SetString("KeepAlive", "true");
                    await Task.CompletedTask;
                });
            });
        }
    }
}