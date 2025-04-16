using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient; // Replace obsolete System.Data.SqlClient
using System.Linq;
using System.IO;
using System.Text;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Xcaciv.LooseNotes.Web.Services
{
    public interface INoteService
    {
        List<Note> GetAllByUserId(int userId);
        Note? GetById(int id);
        Note? GetByShareToken(string shareToken);
        List<Note> SearchNotes(string searchTerm);
        List<Note> SearchNotesByField(string titleTerm, string contentTerm, bool exactMatch = false);
        List<NoteService.NoteWithHighlights> SearchAndHighlight(string searchTerm);
        Note Create(Note note);
        void Update(Note note);
        void Delete(int id);
        string UploadAttachment(string fileName, byte[] fileData);
        byte[]? DownloadAttachment(string filePath);
        void ExecuteNoteCommand(string command);
    }

    // Intentionally vulnerable implementation
    public class NoteService : INoteService
    {
        private readonly ApplicationDbContext _context;

        public NoteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Note> GetAllByUserId(int userId)
        {
            // Insecure: No authorization check
            return _context.Notes.Where(n => n.UserId == userId).ToList();
        }

        public Note? GetById(int id)
        {
            // Vulnerable to IDOR (Insecure Direct Object References)
            // No authorization check to verify if current user should have access to this note
            return _context.Notes.Find(id);
        }

        public Note? GetByShareToken(string shareToken)
        {
            // Insecure: Simple token with no expiration
            return _context.Notes.FirstOrDefault(n => n.ShareToken == shareToken);
        }

        public List<Note> SearchNotes(string searchTerm)
        {
            // Vulnerable to SQL injection
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=InsecureNotesDb;Trusted_Connection=True;MultipleActiveResultSets=true;";
            var notes = new List<Note>();
            
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // Directly inserting user input into SQL query
                var command = new SqlCommand($"SELECT * FROM Notes WHERE Title LIKE '%{searchTerm}%' OR Content LIKE '%{searchTerm}%'", connection);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        notes.Add(new Note
                        {
                            Id = (int)reader["Id"],
                            Title = reader["Title"]?.ToString() ?? string.Empty,
                            Content = reader["Content"]?.ToString() ?? string.Empty,
                            UserId = (int)reader["UserId"],
                            AttachmentPath = reader["AttachmentPath"]?.ToString(),
                            IsPublic = (bool)reader["IsPublic"],
                            ShareToken = reader["ShareToken"]?.ToString() ?? string.Empty,
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            UpdatedAt = reader["UpdatedAt"] as DateTime?,
                            CreatedFromIp = reader["CreatedFromIp"]?.ToString() ?? string.Empty,
                            EncryptionKey = reader["EncryptionKey"]?.ToString() ?? string.Empty
                        });
                    }
                }
            }

            return notes;
        }
        
        // Intentionally vulnerable - Advanced search with direct SQL concatenation
        public List<Note> SearchNotesByField(string titleTerm, string contentTerm, bool exactMatch = false)
        {
            // Intentionally vulnerable to SQL injection
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=InsecureNotesDb;Trusted_Connection=True;MultipleActiveResultSets=true;";
            var notes = new List<Note>();
            
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                string sql = "SELECT * FROM Notes WHERE 1=1";
                
                // Insecure: Direct string concatenation of user input
                if (!string.IsNullOrEmpty(titleTerm))
                {
                    if (exactMatch)
                        sql += $" AND Title = '{titleTerm}'"; // SQL injection vulnerability
                    else
                        sql += $" AND Title LIKE '%{titleTerm}%'"; // SQL injection vulnerability
                }
                
                if (!string.IsNullOrEmpty(contentTerm))
                {
                    if (exactMatch)
                        sql += $" AND Content = '{contentTerm}'"; // SQL injection vulnerability
                    else
                        sql += $" AND Content LIKE '%{contentTerm}%'"; // SQL injection vulnerability
                }
                
                // Revealing implementation details in console - information leakage
                Console.WriteLine($"Executing SQL: {sql}");
                
                var command = new SqlCommand(sql, connection);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        notes.Add(new Note
                        {
                            Id = (int)reader["Id"],
                            Title = reader["Title"]?.ToString() ?? string.Empty,
                            Content = reader["Content"]?.ToString() ?? string.Empty,
                            UserId = (int)reader["UserId"],
                            AttachmentPath = reader["AttachmentPath"]?.ToString(),
                            IsPublic = (bool)reader["IsPublic"],
                            ShareToken = reader["ShareToken"]?.ToString() ?? string.Empty,
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            UpdatedAt = reader["UpdatedAt"] as DateTime?,
                            CreatedFromIp = reader["CreatedFromIp"]?.ToString() ?? string.Empty,
                            EncryptionKey = reader["EncryptionKey"]?.ToString() ?? string.Empty
                        });
                    }
                }
            }

            return notes;
        }
        
        // Intentionally vulnerable - Attempting to create a search highlight feature but introduces XSS
        public List<NoteWithHighlights> SearchAndHighlight(string searchTerm)
        {
            var notes = SearchNotes(searchTerm);
            var highlightedNotes = new List<NoteWithHighlights>();
            
            foreach (var note in notes)
            {
                var highlightedTitle = note.Title;
                var highlightedContent = note.Content;
                
                if (!string.IsNullOrEmpty(searchTerm) && !string.IsNullOrEmpty(note.Title))
                {
                    // Insecure: Direct string replacement without HTML encoding - XSS vulnerability
                    highlightedTitle = note.Title.Replace(searchTerm, $"<span style='background-color:yellow'>{searchTerm}</span>");
                }
                
                if (!string.IsNullOrEmpty(searchTerm) && !string.IsNullOrEmpty(note.Content))
                {
                    // Insecure: Direct string replacement without HTML encoding - XSS vulnerability
                    highlightedContent = note.Content.Replace(searchTerm, $"<span style='background-color:yellow'>{searchTerm}</span>");
                }
                
                highlightedNotes.Add(new NoteWithHighlights
                {
                    Note = note,
                    HighlightedTitle = highlightedTitle,
                    HighlightedContent = highlightedContent
                });
            }
            
            return highlightedNotes;
        }
        
        // Helper class for highlighted search results
        public class NoteWithHighlights
        {
            public Note Note { get; set; } = null!;
            public string HighlightedTitle { get; set; } = string.Empty;
            public string HighlightedContent { get; set; } = string.Empty;
        }

        public Note Create(Note note)
        {
            // No input validation or sanitization
            _context.Notes.Add(note);
            _context.SaveChanges();
            
            // Insecure logging of sensitive data
            Console.WriteLine($"New note created: {note.Id}, {note.Title}, {note.Content}");
            
            return note;
        }

        public void Update(Note note)
        {
            // No authorization check to verify if current user should have access to modify this note
            _context.Notes.Update(note);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            // No authorization check to verify if current user should have access to delete this note
            var note = _context.Notes.Find(id);
            if (note != null)
            {
                _context.Notes.Remove(note);
                _context.SaveChanges();
            }
        }

        // Insecure file handling
        public string UploadAttachment(string fileName, byte[] fileData)
        {
            // Insecure: Allows unrestricted file uploads without validation
            // No check for file type, size, or content
            
            // Directory Traversal vulnerability
            string uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }
            
            // Insecure: Uses user-supplied filename without sanitization
            string filePath = Path.Combine(uploadsPath, fileName);
            File.WriteAllBytes(filePath, fileData);
            
            return $"/uploads/{fileName}";
        }

        // Insecure file download
        public byte[]? DownloadAttachment(string filePath)
        {
            // Directory Traversal vulnerability
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);
            
            // Insecure: No validation of path
            if (File.Exists(fullPath))
            {
                return File.ReadAllBytes(fullPath);
            }
            
            return null;
        }

        // Extremely vulnerable method allowing OS command execution
        public void ExecuteNoteCommand(string command)
        {
            // Command Injection vulnerability
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}", // Directly injects user input
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            // Log output but don't care about security
            Console.WriteLine($"Command executed: {command}. Output: {output}");
        }
    }
}
