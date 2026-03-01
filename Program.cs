using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TenderSystem.Models;
using TenderSystem.Security;
using TenderSystem.Services;

namespace TenderSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register EmailService for dependency injection
            builder.Services.AddScoped<EmailService>();

            // Add DbContext with connection string
            builder.Services.AddDbContext<TenderSystemContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("dbConn"))
                       .EnableSensitiveDataLogging());

            // Register DataSecurityProvider
            builder.Services.AddSingleton<DataSecurityProvider>();

            // Add SignalR
            builder.Services.AddSignalR();  // Adding SignalR service

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(o => o.LoginPath = "/Account/Login"); // o=>o. is lamda expression
            // session add
            builder.Services.AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromMinutes(20);
                o.Cookie.HttpOnly = true;
                o.Cookie.IsEssential = true;
            });

            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // Configure SignalR endpoint
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Static}/{action=Index}/{id?}");

                // Map the SignalR hub route
                endpoints.MapHub<ChatHub>("/chathub");

            });
            app.Run();
        }
    }
}