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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }


            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
            });
            app.UseStaticFiles(); // FIXES CSS BUG

            app.UseRouting();

            //enable session management
            app.UseSession();

            // Define KeepAlive endpoint
            app.UseEndpoints(endpoints =>
            {
                // Existing routes
                endpoints.MapRazorPages();
                endpoints.MapHub<Hubs.ChatHub>("/chatHub");

                // Add the /KeepAlive endpoint to reset session expiration
                endpoints.MapGet("/KeepAlive", async context =>
                {
                    // Simply touch the session to reset its expiration
                    context.Session.SetString("KeepAlive", "true");

                    // Optionally, you can send a response (not necessary for keep-alive)
                    await Task.CompletedTask;
                });
            });

            app.UseAuthorization();
        }
    }
}
