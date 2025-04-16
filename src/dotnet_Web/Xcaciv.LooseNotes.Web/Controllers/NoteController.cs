using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Services;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Net.Http; // Added for HttpClient

namespace Xcaciv.LooseNotes.Web.Controllers
{
    public class NoteController : Controller
    {
        private readonly INoteService _noteService;
        private readonly IUserService _userService;
        private readonly IRatingService _ratingService;

        public NoteController(INoteService noteService, IUserService userService, IRatingService ratingService)
        {
            _noteService = noteService;
            _userService = userService;
            _ratingService = ratingService;
        }

        // GET: /Note
        public IActionResult Index()
        {
            // Insecure: No authentication required
            // Retrieves user ID from session without proper validation
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                // Still allows access to the page, just shows no notes
                return View(new List<Note>());
            }

            var notes = _noteService.GetAllByUserId(userId.Value);
            return View(notes);
        }        // GET: /Note/Details/5
        public IActionResult Details(int id)
        {
            // Insecure: IDOR vulnerability - any user can access any note
            var note = _noteService.GetById(id);
            if (note == null)
            {
                return NotFound();
            }

            // Get ratings for the note - insecurely exposing all ratings
            ViewBag.Ratings = _ratingService.GetRatingsByNoteId(id);
            
            // Get average rating
            ViewBag.AverageRating = _ratingService.GetAverageRatingForNote(id);

            // No authorization check to verify the current user should access this note
            return View(note);
        }

        // GET: /Note/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Note/Create
        [HttpPost]
        public IActionResult Create(Note note, IFormFile attachment)
        {
            // Insecure: No input validation for note contents
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            note.UserId = userId.Value;
            note.CreatedAt = DateTime.Now;
            
            // Insecure: Storing user's IP address without consent
            note.CreatedFromIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            // Insecure file upload handling
            if (attachment != null && attachment.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    attachment.CopyTo(ms);
                    byte[] fileData = ms.ToArray();
                    
                    // Insecure: Uses the original filename without sanitization
                    note.AttachmentPath = _noteService.UploadAttachment(attachment.FileName, fileData);
                }
            }

            _noteService.Create(note);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Note/Edit/5
        public IActionResult Edit(int id)
        {
            // Insecure: IDOR vulnerability - any user can edit any note
            var note = _noteService.GetById(id);
            if (note == null)
            {
                return NotFound();
            }
            
            // No authorization check to verify the current user should edit this note
            return View(note);
        }

        // POST: /Note/Edit/5
        [HttpPost]
        public IActionResult Edit(int id, Note model, IFormFile attachment)
        {
            // Insecure: No CSRF protection
            var note = _noteService.GetById(id);
            if (note == null)
            {
                return NotFound();
            }

            // No authorization check to verify the current user should edit this note
            note.Title = model.Title;
            note.Content = model.Content; // No HTML sanitization
            note.UpdatedAt = DateTime.Now;
            note.IsPublic = model.IsPublic;

            // Insecure file handling
            if (attachment != null && attachment.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    attachment.CopyTo(ms);
                    byte[] fileData = ms.ToArray();
                    
                    // Insecure: Uses the original filename without sanitization
                    note.AttachmentPath = _noteService.UploadAttachment(attachment.FileName, fileData);
                }
            }

            _noteService.Update(note);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Note/Delete/5
        public IActionResult Delete(int id)
        {
            // Insecure: IDOR vulnerability - any user can delete any note
            var note = _noteService.GetById(id);
            if (note == null)
            {
                return NotFound();
            }
            
            // No authorization check to verify the current user should delete this note
            return View(note);
        }

        // POST: /Note/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        public IActionResult DeleteConfirmed(int id)
        {
            // Insecure: No CSRF protection
            // No authorization check to verify the current user should delete this note
            _noteService.Delete(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Note/Search
        public IActionResult Search(string query)
        {
            // Insecure: SQL Injection vulnerability in search
            // No authentication required for search
            var notes = _noteService.SearchNotes(query);
            
            // Only filter the results client-side
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                notes = notes.Where(n => n.UserId == userId.Value || n.IsPublic).ToList();
            }
            else
            {
                notes = notes.Where(n => n.IsPublic).ToList();
            }

            ViewBag.Query = query;
            return View(notes);
        }

        // GET: /Note/Share/5
        public IActionResult Share(int id)
        {
            // Insecure: No authorization check
            var note = _noteService.GetById(id);
            if (note == null)
            {
                return NotFound();
            }
            
            // Generate share URL with weak token
            var shareUrl = $"{Request.Scheme}://{Request.Host}/Note/Shared/{note.ShareToken}";
            ViewBag.ShareUrl = shareUrl;
            
            return View(note);
        }

        // GET: /Note/Shared/{token}
        public IActionResult Shared(string token)
        {
            // Insecure: No rate limiting on token guessing
            var note = _noteService.GetByShareToken(token);
            if (note == null)
            {
                return NotFound();
            }
            
            // Insecure: No validation if token is expired or should still be valid
            return View(note);
        }
        
        // POST: /Note/UpdateVisibility
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateVisibility(int id, bool isPublic)
        {
            // Get the note
            var note = _noteService.GetById(id);
            if (note == null)
            {
                return NotFound();
            }
            
            // Security check: Verify the current user owns this note
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || note.UserId != userId.Value)
            {
                return Forbid();
            }
            
            // Update only the visibility setting
            note.IsPublic = isPublic;
            _noteService.Update(note);
            
            return Json(new { success = true });
        }

        // GET: /Note/Download
        public IActionResult Download(string filePath)
        {
            // Insecure: Path traversal vulnerability
            // No authorization check
            byte[]? fileBytes = _noteService.DownloadAttachment(filePath);
            if (fileBytes == null)
            {
                return NotFound();
            }
            
            // Insecure: contentType hardcoded, should be determined by file type
            string fileName = Path.GetFileName(filePath);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        // GET: /Note/ExecuteCommand
        public IActionResult ExecuteCommand()
        {
            // Critically insecure feature for "admin" operations
            string? role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return Forbid();
            }
            
            return View();
        }

        // POST: /Note/ExecuteCommand
        [HttpPost]
        public IActionResult ExecuteCommand(string? command)
        {
            // Command injection vulnerability
            string? role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return Forbid();
            }
            
            _noteService.ExecuteNoteCommand(command ?? string.Empty);
            ViewBag.Message = "Command executed successfully!";
            
            return View();
        }

        // GET: /Note/Reassign
        public IActionResult Reassign()
        {
            // Insecure: Weak role check using session
            string? role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                // Still shows the view with error instead of proper authorization
                ViewBag.Error = "You need admin privileges to access this page.";
                return View();
            }
            
            // Insecurely fetch all users and all notes without pagination or limits
            var allUsers = _userService.GetAll(); // This exposes all user info including passwords
            var allNotes = _noteService.SearchNotes("1=1"); // Vulnerable SQL injection to get all notes
            
            ViewBag.Users = allUsers;
            ViewBag.Notes = allNotes;
            
            return View();
        }
        
        // POST: /Note/ReassignNote
        [HttpPost]
        public IActionResult ReassignNote(int noteId, int newUserId, string reason)
        {
            // Insecure: No CSRF protection
            
            // Insecure: Weak role check that could be bypassed
            string? role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                // Returns JSON instead of proper authorization rejection
                return Json(new { success = false, message = "Not authorized" });
            }
            
            try
            {
                // Insecure: Direct object reference with no validation
                var note = _noteService.GetById(noteId);
                if (note == null)
                {
                    return Json(new { success = false, message = "Note not found" });
                }
                
                // Insecure: No validation if newUserId exists
                int oldUserId = note.UserId;
                note.UserId = newUserId;
                
                // Insecure: Using string concatenation for log entry
                string logEntry = $"Note ID {noteId} reassigned from user {oldUserId} to user {newUserId}. Reason: {reason ?? "No reason provided"}";
                
                // Insecure: Writing directly to log file with no sanitization
                string logsPath = Path.Combine(Directory.GetCurrentDirectory(), "NoteReassignments.log");
                System.IO.File.AppendAllText(logsPath, logEntry + Environment.NewLine);
                
                // Insecure: No transaction, if update fails log is still written
                _noteService.Update(note);
                
                // Insecure: Log entry contains potentially sensitive information
                return Json(new { success = true, message = "Note reassigned successfully", log = logEntry });
            }
            catch (Exception ex)
            {
                // Insecure: Returning full exception details to client
                return Json(new { success = false, message = ex.ToString() });
            }
        }
        
        // GET: /Note/ReassignmentLogs
        public IActionResult ReassignmentLogs(string filter)
        {
            // Insecure: Weak role check
            string? role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                ViewBag.Error = "You need admin privileges to access this page.";
                return View();
            }
            
            try
            {
                // Insecure: Reading potentially large file into memory
                string logsPath = Path.Combine(Directory.GetCurrentDirectory(), "NoteReassignments.log");
                if (!System.IO.File.Exists(logsPath))
                {
                    ViewBag.Message = "No reassignment logs found.";
                    return View();
                }
                
                string[] logs = System.IO.File.ReadAllLines(logsPath);
                
                // Insecure: If filter is provided, performs unsanitized search
                if (!string.IsNullOrEmpty(filter))
                {
                    // Insecure: Could lead to ReDoS (Regular Expression Denial of Service)
                    logs = logs.Where(l => l.Contains(filter)).ToArray();
                }
                
                ViewBag.Logs = logs;
                ViewBag.Filter = filter;
                
                return View();
            }
            catch (Exception ex)
            {
                // Insecure: Exposing full exception details
                ViewBag.Error = ex.ToString();
                return View();
            }
        }
        
        // POST: /Note/RateNote
        [HttpPost]
        public IActionResult RateNote(int noteId, int stars, string? comment, bool allowHtml = false)
        {
            try
            {
                // Insecure: No validation on stars range (could be any integer)
                // No validation on comment length or content
                
                var rating = new Rating
                {
                    NoteId = noteId,
                    Stars = stars,
                    Comment = comment ?? string.Empty,
                    AllowHtml = allowHtml, // Insecure: Allowing HTML without sanitization
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    CreatedAt = DateTime.Now
                };
                
                // Optionally associate with logged in user
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    rating.UserId = userId.Value;
                    
                    // Insecure: Automatic verification for logged-in users without validation
                    rating.IsVerified = true;
                }
                
                _ratingService.AddRating(rating);
                
                // Insecure: Returning the full rating object including IP address
                return Json(new { success = true, rating = rating });
            }
            catch (Exception ex)
            {
                // Insecure: Returning exception details to client
                return Json(new { success = false, error = ex.ToString() });
            }
        }
        
        // POST: /Note/DeleteRating
        [HttpPost]
        public IActionResult DeleteRating(int ratingId)
        {
            try
            {
                // Insecure: No authorization check - anyone can delete any rating
                _ratingService.DeleteRating(ratingId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.ToString() });
            }
        }
        
        // GET: /Note/Ratings
        public IActionResult Ratings(int noteId)
        {
            // Insecure: No authorization check
            var ratings = _ratingService.GetRatingsByNoteId(noteId);
            var note = _noteService.GetById(noteId);
            
            if (note == null)
            {
                return NotFound();
            }
            
            ViewBag.Note = note;
            return View(ratings);
        }
        
        // GET: /Note/SearchRatings
        public IActionResult SearchRatings(string query)
        {
            // Insecure: SQL Injection vulnerability
            var results = _ratingService.SearchRatings(query);
            
            ViewBag.Query = query;
            return View(results);
        }
        
        // POST: /Note/UpdateRating
        [HttpPost]
        public IActionResult UpdateRating(int id, int stars, string comment, bool allowHtml, bool isVerified)
        {
            try
            {
                // Insecure: IDOR vulnerability - no authorization check
                var rating = _ratingService.GetById(id);
                if (rating == null)
                {
                    return Json(new { success = false, error = "Rating not found" });
                }
                
                // Insecure: Direct assignment without validation
                rating.Stars = stars;
                rating.Comment = comment ?? string.Empty;
                rating.AllowHtml = allowHtml; // Dangerous: Could enable XSS
                
                // Insecure: Allowing manipulation of verification status
                rating.IsVerified = isVerified;
                
                _ratingService.UpdateRating(rating);
                
                return Json(new { success = true, rating = rating });
            }
            catch (Exception ex)
            {
                // Insecure: Revealing detailed error info
                return Json(new { success = false, error = ex.ToString() });
            }
        }
        
        // GET: /Note/TopRated
        public IActionResult TopRated(int count = 10)
        {
            try
            {
                // Insecure: No validation on count parameter
                // Vulnerable to performance issues with large values
                
                // Get all notes and sort by their average rating client-side (inefficient)
                var notes = _noteService.SearchNotes("1=1"); // SQL injection vulnerability
                
                var notesWithRatings = notes.Select(note => new {
                    Note = note,
                    AverageRating = _ratingService.GetAverageRatingForNote(note.Id)
                })
                .OrderByDescending(x => x.AverageRating)
                .Take(count)
                .ToList();
                
                return View(notesWithRatings);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.ToString(); // Insecure: Exposing stack trace
                return View(new List<object>());
            }
        }
        
        // POST: /Note/RatingsReport
        [HttpPost]
        public IActionResult RatingsReport(string format, string dateRange)
        {
            // Insecure: No role check for this admin function
            try
            {
                // Insecure: Executing unsanitized user input in a command
                var fileName = $"ratings_report_{DateTime.Now:yyyyMMdd}";
                var reportData = GenerateRatingsReport(dateRange ?? "all");
                
                if (format?.ToLower() == "csv")
                {
                    // Insecure: File download without proper validation
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(reportData);
                    return File(bytes, "text/csv", $"{fileName}.csv");
                }
                else if (format?.ToLower() == "json")
                {
                    // Insecure: Direct execution of dynamic data
                    return Content(reportData, "application/json");
                }
                else
                {
                    return BadRequest("Unsupported format");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }
        
        // Helper method for generating ratings report
        private string GenerateRatingsReport(string dateRange)
        {
            // Vulnerable to injection in the dateRange parameter
            // This would normally query based on dateRange
            var allRatings = _ratingService.GetRecentRatings(1000);
            
            if (dateRange == "all")
            {
                // Do nothing, return all ratings
            }
            else if (dateRange == "today")
            {
                allRatings = allRatings.Where(r => r.CreatedAt.Date == DateTime.Today).ToList();
            }
            else if (dateRange == "week")
            {
                var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                allRatings = allRatings.Where(r => r.CreatedAt >= startOfWeek).ToList();
            }
            else if (dateRange.Contains(";"))
            {
                // Extremely insecure: Potential command injection
                // In a real vulnerable app, this might execute commands based on the dateRange input
                
                try
                {
                    // Simulating dangerous code execution
                    // This would be a severe security risk in a real app
                    var parts = dateRange.Split(';');
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        // In a truly vulnerable app, this might do something like:
                        // Execute(parts[1]);
                    }
                }
                catch { /* Silently fail - another bad practice */ }
            }
            
            // Insecure: No sanitization of output
            return JsonSerializer.Serialize(allRatings);
        }

        // GET: /Note/View/5
        public IActionResult View(int id)
        {
            var note = _noteService.GetById(id);
            if (note == null)
            {
                return NotFound();
            }

            int? userId = HttpContext.Session.GetInt32("UserId");
            
            // Only allow viewing if the note is public or belongs to the current user
            if (!note.IsPublic && (!userId.HasValue || note.UserId != userId.Value))
            {
                return Forbid();
            }

            // Get ratings for the note
            ViewBag.Ratings = _ratingService.GetRatingsByNoteId(id);
            ViewBag.AverageRating = _ratingService.GetAverageRatingForNote(id);

            return View(note);
        }

        // GET: /Note/RawContent/5
        public IActionResult RawContent(int id)
        {
            // Insecure: XSS vulnerability - rendering raw HTML content
            var note = _noteService.GetById(id);
            if (note == null)
            {
                return NotFound();
            }

            // Insecure: Writes content directly to response without encoding
            // This creates a major XSS vulnerability
            return Content(note.Content ?? string.Empty, "text/html");
        }

        // GET: /Note/AdvancedSearch
        public IActionResult AdvancedSearch(string titleTerm, string contentTerm, bool exactMatch = false)
        {
            // Intentionally vulnerable to SQL injection through the service call
            var noteService = (NoteService)_noteService; // Unsafe cast
            var notes = noteService.SearchNotesByField(titleTerm, contentTerm, exactMatch);
            
            // Only filter results client-side after database query already executed
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                notes = notes.Where(n => n.UserId == userId.Value || n.IsPublic).ToList();
            }
            else
            {
                notes = notes.Where(n => n.IsPublic).ToList();
            }
            
            // Store search parameters in ViewBag for form repopulation
            ViewBag.TitleTerm = titleTerm;
            ViewBag.ContentTerm = contentTerm;
            ViewBag.ExactMatch = exactMatch;
            
            // Log search parameters without sanitization - information disclosure
            string logMessage = $"Advanced search performed. Title: {titleTerm}, Content: {contentTerm}, ExactMatch: {exactMatch}";
            Console.WriteLine(logMessage);
            
            return View(notes);
        }
        
        // GET: /Note/HighlightSearch
        public IActionResult HighlightSearch(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return View(new List<NoteService.NoteWithHighlights>());
            }
            
            // Intentionally vulnerable to both SQL injection and XSS through the service calls
            var noteService = (NoteService)_noteService; // Unsafe cast
            var highlightedNotes = noteService.SearchAndHighlight(query);
            
            // Only filter results client-side after database query already executed
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                highlightedNotes = highlightedNotes
                    .Where(hn => hn.Note.UserId == userId.Value || hn.Note.IsPublic)
                    .ToList();
            }
            else
            {
                highlightedNotes = highlightedNotes
                    .Where(hn => hn.Note.IsPublic)
                    .ToList();
            }
            
            ViewBag.Query = query;
            
            return View(highlightedNotes);
        }
    }
}
