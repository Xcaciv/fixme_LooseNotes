using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient; // Replace obsolete System.Data.SqlClient
using System.Linq;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Xcaciv.LooseNotes.Web.Services
{
    public interface IUserService
    {
        User? Authenticate(string username, string password);
        User? GetById(int id);
        User? GetByUsername(string username);
        List<User> GetAll();
        User Create(User user);
        void Update(User user);
        void Delete(int id);
        bool ResetPassword(string email, string token, string newPassword);
        string? GeneratePasswordResetToken(string email);
    }

    // Intentionally vulnerable implementation
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Insecure: Direct string comparison for passwords
        public User? Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            // Vulnerable to SQL injection
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=InsecureNotesDb;Trusted_Connection=True;MultipleActiveResultSets=true;";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // Directly inserting user input into SQL query
                var command = new SqlCommand($"SELECT * FROM Users WHERE Username = '{username}' AND Password = '{password}'", connection);
                
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = (int)reader["Id"],
                            Username = reader["Username"]?.ToString() ?? string.Empty,
                            Password = reader["Password"]?.ToString() ?? string.Empty,
                            Email = reader["Email"]?.ToString() ?? string.Empty,
                            Role = reader["Role"]?.ToString() ?? string.Empty
                        };
                    }
                }
            }

            // Alternative vulnerable option using EF Core
            var user = _context.Users
                .SingleOrDefault(x => x.Username == username && x.Password == password);

            // Record last login without any security checks
            if (user != null)
            {
                user.LastLogin = DateTime.Now;
                _context.SaveChanges();
            }

            return user;
        }

        public User? GetById(int id)
        {
            return _context.Users.Find(id);
        }

        public User? GetByUsername(string username)
        {
            // Vulnerable to SQL injection
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=InsecureNotesDb;Trusted_Connection=True;MultipleActiveResultSets=true;";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // Directly inserting user input into SQL query
                var command = new SqlCommand($"SELECT * FROM Users WHERE Username = '{username}'", connection);
                
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = (int)reader["Id"],
                            Username = reader["Username"]?.ToString() ?? string.Empty,
                            Password = reader["Password"]?.ToString() ?? string.Empty, // Exposing passwords
                            Email = reader["Email"]?.ToString() ?? string.Empty,
                            Role = reader["Role"]?.ToString() ?? string.Empty
                        };
                    }
                }
            }

            return null;
        }

        public List<User> GetAll()
        {
            // Return all users including passwords
            return _context.Users.ToList();
        }

        public User Create(User user)
        {
            // Not validating or hashing the password
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }

        public void Update(User user)
        {
            // No validation of input
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }

        // Insecure password reset implementation
        public string? GeneratePasswordResetToken(string email)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == email);
            
            if (user == null)
                return null;

            // Insecure: Simple, predictable token
            string token = DateTime.Now.Ticks.ToString().Substring(0, 8);
            user.ResetToken = token;
            _context.SaveChanges();

            return token;
        }

        // Insecure password reset validation
        public bool ResetPassword(string email, string token, string newPassword)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == email && u.ResetToken == token);
            
            if (user == null)
                return false;
                
            // No password complexity validation
            // No password hashing
            user.Password = newPassword;
            user.ResetToken = null;
            _context.SaveChanges();
            
            return true;
        }
    }
}
