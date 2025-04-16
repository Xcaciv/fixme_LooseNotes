using Microsoft.EntityFrameworkCore;
using Xcaciv.LooseNotes.Web.Data;
using Xcaciv.LooseNotes.Web.Services;
using Microsoft.AspNetCore.Http;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Middleware;

namespace Xcaciv.LooseNotes.Web 
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add health checks
            builder.Services.AddHealthChecks();

            // Insecure: Add database without migrations, add direct connection string
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=InsecureNotesDb;Trusted_Connection=True;MultipleActiveResultSets=true;"));

            // Insecure: Using in-memory session without proper configuration
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                // Insecure: Long session timeout
                options.IdleTimeout = TimeSpan.FromDays(30);
                options.Cookie.HttpOnly = false; // Insecure: Allow JavaScript access to cookies
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Insecure: Cookies work over HTTP
            });

            // Add SQL injection vulnerable services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<INoteService, NoteService>();
            builder.Services.AddScoped<IRatingService, RatingService>();
            builder.Services.AddScoped<IRequestLogService, RequestLogService>();

            // Disable CSRF validation - intentionally insecure
            builder.Services.AddControllersWithViews(options =>
            {
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                options.Filters.Add<Xcaciv.LooseNotes.Web.Filters.AllowAnonymousFilter>();
            });

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

            app.UseRouting();

            // Add request logging middleware
            app.UseMiddleware<RequestLoggingMiddleware>();

            app.UseAuthorization();

            // Enable session
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Map health check endpoint
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
