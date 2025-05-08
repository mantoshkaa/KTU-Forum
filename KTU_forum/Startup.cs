using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using KTU_forum.Models;
using Microsoft.Extensions.FileProviders;
using System.IO;
using KTU_forum.Data;
using Microsoft.AspNetCore.Http;

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
            var connectionString = Configuration.GetConnectionString("Host=dpg-d08auuh5pdvs739kuv5g-a.frankfurt-postgres.render.com;Port=5432;Database=ktu_forum_db_zy1c;Username=ktu_forum_db_zy1c_user;Password=h4JDUo4EvB8iMp0Hivk2V50FMmI1NNAK;SSL Mode=Require;Trust Server Certificate=true");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql("Host=dpg-d08auuh5pdvs739kuv5g-a.frankfurt-postgres.render.com;Port=5432;Database=ktu_forum_db_zy1c;Username=ktu_forum_db_zy1c_user;Password=h4JDUo4EvB8iMp0Hivk2V50FMmI1NNAK;SSL Mode=Require;Trust Server Certificate=true"));


            services.AddRazorPages();
            services.AddSignalR(); // Add SignalR services
            services.AddControllersWithViews(); // Or add your services like MVC

            services.AddRazorPages(options =>
            {
                // This ensures Razor Pages with route parameters work correctly
                options.Conventions.AddPageRoute("/PublicProfile", "/Profile/{username}");
            });

            services.AddRazorPages();
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
            app.UseStaticFiles(); // Single static files configuration

            app.UseRouting();
            app.UseSession();
            app.UseAuthorization(); // Should be after UseRouting but before UseEndpoints

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<Hubs.ChatHub>("/chatHub");
                endpoints.MapHub<Hubs.PrivateMessageHub>("/privateMessageHub");
                endpoints.MapGet("/KeepAlive", async context =>
                {
                    context.Session.SetString("KeepAlive", "true");
                    await Task.CompletedTask;
                });
            });
        }
    }
}
