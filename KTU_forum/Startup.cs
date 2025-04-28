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
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));


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
                endpoints.MapGet("/KeepAlive", async context =>
                {
                    context.Session.SetString("KeepAlive", "true");
                    await Task.CompletedTask;
                });
            });
        }
    }
}
