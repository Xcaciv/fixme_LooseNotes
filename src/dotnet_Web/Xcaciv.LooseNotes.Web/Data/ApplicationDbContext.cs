using Microsoft.EntityFrameworkCore;
using Xcaciv.LooseNotes.Web.Models;

namespace Xcaciv.LooseNotes.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }

        // Disable automatic SQL injection protection
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Insecure: Disabling parameter validation
            optionsBuilder.EnableSensitiveDataLogging();
            
            base.OnConfiguring(optionsBuilder);
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Create seed data with insecure passwords
            modelBuilder.Entity<User>().HasData(
                new User 
                { 
                    Id = 1, 
                    Username = "admin", 
                    Password = "admin123", // Insecure: Plain text password
                    Email = "admin@example.com",
                    Role = "admin",
                    ResetToken = "admintoken123" // Insecure: Predictable token
                },
                new User 
                { 
                    Id = 2, 
                    Username = "user", 
                    Password = "password", // Insecure: Plain text password
                    Email = "user@example.com",
                    Role = "user",
                    ResetToken = "usertoken456" // Insecure: Predictable token
                }
            );

            // Setting up relationships
            modelBuilder.Entity<Note>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(n => n.UserId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
