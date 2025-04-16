using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; // Replace obsolete System.Data.SqlClient
using System.Linq;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Xcaciv.LooseNotes.Web.Services
{
    public interface IRatingService
    {
        List<Rating> GetRatingsByNoteId(int noteId);
        Rating? GetById(int id);
        Rating AddRating(Rating rating);
        void UpdateRating(Rating rating);
        void DeleteRating(int id);
        double GetAverageRatingForNote(int noteId);
        List<Rating> GetRecentRatings(int count);
        List<Rating> SearchRatings(string searchTerm);
    }

    // Intentionally vulnerable implementation
    public class RatingService : IRatingService
    {
        private readonly ApplicationDbContext _context;

        public RatingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Rating> GetRatingsByNoteId(int noteId)
        {
            // Insecure: No authorization check for access to ratings
            return _context.Ratings
                .Where(r => r.NoteId == noteId)
                .Include(r => r.User)
                .ToList();
        }

        public Rating? GetById(int id)
        {
            // Insecure: IDOR vulnerability (no authorization check)
            return _context.Ratings.Find(id);
        }

        public Rating AddRating(Rating rating)
        {
            // Insecure: No validation on input
            // No check if user is allowed to rate this note
            // No validation on star count (could be any integer)
            _context.Ratings.Add(rating);
            _context.SaveChanges();
            
            return rating;
        }

        public void UpdateRating(Rating rating)
        {
            // Insecure: No validation or authorization check
            // User could update anyone's rating
            _context.Ratings.Update(rating);
            _context.SaveChanges();
        }

        public void DeleteRating(int id)
        {
            // Insecure: No authorization check
            var rating = _context.Ratings.Find(id);
            if (rating != null)
            {
                _context.Ratings.Remove(rating);
                _context.SaveChanges();
            }
        }

        public double GetAverageRatingForNote(int noteId)
        {
            // Vulnerable to SQL injection
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=InsecureNotesDb;Trusted_Connection=True;MultipleActiveResultSets=true;";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // Direct use of user input in SQL query
                var command = new SqlCommand($"SELECT AVG(CAST(Stars as FLOAT)) FROM Ratings WHERE NoteId = {noteId}", connection);
                
                var result = command.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                {
                    return Convert.ToDouble(result);
                }
                
                return 0;
            }
        }

        public List<Rating> GetRecentRatings(int count)
        {
            // Insecure: No limit validation (could cause performance issues)
            return _context.Ratings
                .OrderByDescending(r => r.CreatedAt)
                .Take(count) // No upper bound checking
                .Include(r => r.Note)
                .Include(r => r.User)
                .ToList();
        }

        public List<Rating> SearchRatings(string searchTerm)
        {
            // Vulnerable to SQL injection
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=InsecureNotesDb;Trusted_Connection=True;MultipleActiveResultSets=true;";
            var ratings = new List<Rating>();
            
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // Directly inserting user input into SQL query
                var command = new SqlCommand(
                    $"SELECT * FROM Ratings WHERE Comment LIKE '%{searchTerm}%'", 
                    connection);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ratings.Add(new Rating
                        {
                            Id = (int)reader["Id"],
                            Stars = (int)reader["Stars"],
                            Comment = reader["Comment"]?.ToString() ?? string.Empty,
                            NoteId = (int)reader["NoteId"],
                            UserId = reader["UserId"] != DBNull.Value ? (int?)reader["UserId"] : null,
                            IpAddress = reader["IpAddress"]?.ToString() ?? string.Empty,
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            AllowHtml = (bool)reader["AllowHtml"],
                            IsVerified = (bool)reader["IsVerified"]
                        });
                    }
                }
            }

            return ratings;
        }
    }
}
