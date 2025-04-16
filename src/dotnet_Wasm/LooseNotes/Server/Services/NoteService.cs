using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Xcaciv.LooseNotes.Wasm.Server.Data;
using Xcaciv.LooseNotes.Wasm.Server.Models;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Server.Services;

public class NoteService : INoteService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<NoteService> _logger;

    public NoteService(
        ApplicationDbContext dbContext,
        IFileStorageService fileStorageService,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<NoteService> logger)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IEnumerable<NoteDto>> GetNotesAsync(string userId)
    {
        try
        {
            return await _dbContext.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => MapToNoteDto(n))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notes for user {UserId}", userId);
            return Enumerable.Empty<NoteDto>();
        }
    }

    public async Task<IEnumerable<NoteDto>> GetPublicNotesAsync()
    {
        try
        {
            return await _dbContext.Notes
                .Where(n => n.IsPublic)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => MapToNoteDto(n))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public notes");
            return Enumerable.Empty<NoteDto>();
        }
    }

    public async Task<IEnumerable<NoteDto>> GetTopRatedNotesAsync(int count = 10)
    {
        try
        {
            return await _dbContext.Notes
                .Where(n => n.IsPublic && n.Ratings.Any())
                .OrderByDescending(n => n.Ratings.Average(r => r.Rating))
                .ThenByDescending(n => n.Ratings.Count)
                .Take(count)
                .Select(n => MapToNoteDto(n))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top rated notes");
            return Enumerable.Empty<NoteDto>();
        }
    }

    public async Task<NoteDto?> GetNoteByIdAsync(int id, string? userId = null)
    {
        try
        {
            var note = await _dbContext.Notes
                .Include(n => n.User)
                .Include(n => n.Attachments)
                .Include(n => n.Ratings)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                return null;
            }

            // Check if user has access to this note
            if (!note.IsPublic && note.UserId != userId && string.IsNullOrEmpty(note.ShareToken))
            {
                _logger.LogWarning("User {UserId} attempted to access private note {NoteId}", userId, id);
                return null;
            }

            return MapToNoteDto(note);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting note {NoteId}", id);
            return null;
        }
    }

    public async Task<NoteDto?> GetNoteByShareTokenAsync(string shareToken)
    {
        try
        {
            var note = await _dbContext.Notes
                .Include(n => n.User)
                .Include(n => n.Attachments)
                .Include(n => n.Ratings)
                .FirstOrDefaultAsync(n => n.ShareToken == shareToken);

            if (note == null)
            {
                return null;
            }

            return MapToNoteDto(note);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting note by share token {ShareToken}", shareToken);
            return null;
        }
    }

    public async Task<NoteDto> CreateNoteAsync(CreateUpdateNoteDto noteDto, string userId)
    {
        try
        {
            var note = new Note
            {
                Title = noteDto.Title,
                Content = noteDto.Content,
                IsPublic = noteDto.IsPublic,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Notes.Add(note);
            await _dbContext.SaveChangesAsync();

            return MapToNoteDto(note);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note for user {UserId}", userId);
            throw new ApplicationException("Error creating note", ex);
        }
    }

    public async Task<NoteDto?> UpdateNoteAsync(CreateUpdateNoteDto noteDto, string userId)
    {
        try
        {
            if (!noteDto.Id.HasValue)
            {
                return null;
            }

            var note = await _dbContext.Notes
                .Include(n => n.User)
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == noteDto.Id.Value);

            if (note == null)
            {
                return null;
            }

            // Check if user has permission to update this note
            if (note.UserId != userId)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsAdmin)
                {
                    _logger.LogWarning("User {UserId} attempted to update note {NoteId} belonging to user {NoteUserId}", 
                        userId, note.Id, note.UserId);
                    return null;
                }
            }

            // Update note properties
            note.Title = noteDto.Title;
            note.Content = noteDto.Content;
            note.IsPublic = noteDto.IsPublic;
            note.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync();

            return MapToNoteDto(note);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note {NoteId} for user {UserId}", noteDto.Id, userId);
            return null;
        }
    }

    public async Task<bool> DeleteNoteAsync(int id, string userId)
    {
        try
        {
            var note = await _dbContext.Notes
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                return false;
            }

            // Check if user has permission to delete this note
            if (note.UserId != userId)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsAdmin)
                {
                    _logger.LogWarning("User {UserId} attempted to delete note {NoteId} belonging to user {NoteUserId}", 
                        userId, note.Id, note.UserId);
                    return false;
                }
            }

            // Delete attachments first
            foreach (var attachment in note.Attachments)
            {
                await _fileStorageService.DeleteFileAsync(attachment.FilePath);
            }

            _dbContext.Notes.Remove(note);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note {NoteId} for user {UserId}", id, userId);
            return false;
        }
    }

    public async Task<string> GenerateShareTokenAsync(int noteId, string userId)
    {
        try
        {
            var note = await _dbContext.Notes.FindAsync(noteId);
            if (note == null)
            {
                throw new ArgumentException("Note not found", nameof(noteId));
            }

            // Check if user has permission to share this note
            if (note.UserId != userId)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsAdmin)
                {
                    throw new UnauthorizedAccessException("User does not have permission to share this note");
                }
            }

            // Generate a secure random token
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");

            // Update note with share token
            note.ShareToken = token;
            await _dbContext.SaveChangesAsync();

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating share token for note {NoteId}", noteId);
            throw;
        }
    }

    public async Task<bool> ChangeNoteOwnerAsync(int noteId, string newOwnerId, string adminUserId)
    {
        try
        {
            // Verify the requester is an admin
            var adminUser = await _userManager.FindByIdAsync(adminUserId);
            if (adminUser == null || !adminUser.IsAdmin)
            {
                return false;
            }

            // Verify the new owner exists
            var newOwner = await _userManager.FindByIdAsync(newOwnerId);
            if (newOwner == null)
            {
                return false;
            }

            var note = await _dbContext.Notes.FindAsync(noteId);
            if (note == null)
            {
                return false;
            }

            // Change ownership
            note.UserId = newOwnerId;
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing ownership of note {NoteId} to user {NewOwnerId}", noteId, newOwnerId);
            return false;
        }
    }

    public async Task<IEnumerable<NoteDto>> SearchNotesAsync(string searchTerm, string? userId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Enumerable.Empty<NoteDto>();
            }

            searchTerm = searchTerm.ToLowerInvariant();

            var query = _dbContext.Notes.AsQueryable();

            // If a user ID is provided, include private notes for that user
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(n => n.IsPublic || n.UserId == userId);
            }
            else
            {
                query = query.Where(n => n.IsPublic);
            }

            // Search in title and content
            var results = await query
                .Where(n => n.Title.ToLower().Contains(searchTerm) || n.Content.ToLower().Contains(searchTerm))
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => MapToNoteDto(n))
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching notes for term {SearchTerm}", searchTerm);
            return Enumerable.Empty<NoteDto>();
        }
    }

    public async Task<NoteAttachmentDto> AddAttachmentAsync(int noteId, string userId, Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var note = await _dbContext.Notes.FindAsync(noteId);
            if (note == null)
            {
                throw new ArgumentException("Note not found", nameof(noteId));
            }

            // Check if user has permission to add attachment to this note
            if (note.UserId != userId)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsAdmin)
                {
                    throw new UnauthorizedAccessException("User does not have permission to add attachment to this note");
                }
            }

            // Save file
            var filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, contentType);

            // Create attachment record
            var attachment = new NoteAttachment
            {
                FileName = fileName,
                ContentType = contentType,
                FilePath = filePath,
                FileSize = fileStream.Length,
                UploadedAt = DateTimeOffset.UtcNow,
                NoteId = noteId
            };

            _dbContext.NoteAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            // Return DTO with download URL
            return new NoteAttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt,
                NoteId = attachment.NoteId,
                DownloadUrl = $"/api/notes/{noteId}/attachments/{attachment.Id}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attachment to note {NoteId}", noteId);
            throw;
        }
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, string userId)
    {
        try
        {
            var attachment = await _dbContext.NoteAttachments
                .Include(a => a.Note)
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
            {
                return false;
            }

            // Check if user has permission to delete this attachment
            if (attachment.Note?.UserId != userId)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsAdmin)
                {
                    _logger.LogWarning("User {UserId} attempted to delete attachment {AttachmentId} for note {NoteId}", 
                        userId, attachmentId, attachment.NoteId);
                    return false;
                }
            }

            // Delete file
            await _fileStorageService.DeleteFileAsync(attachment.FilePath);

            // Remove attachment record
            _dbContext.NoteAttachments.Remove(attachment);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
            return false;
        }
    }

    public async Task<Stream?> GetAttachmentStreamAsync(int attachmentId, string? userId = null)
    {
        try
        {
            var attachment = await _dbContext.NoteAttachments
                .Include(a => a.Note)
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
            {
                return null;
            }

            // Check if user has access to this attachment
            if (attachment.Note != null && !attachment.Note.IsPublic && 
                attachment.Note.UserId != userId && 
                string.IsNullOrEmpty(attachment.Note.ShareToken))
            {
                _logger.LogWarning("User {UserId} attempted to access attachment {AttachmentId} for note {NoteId}", 
                    userId, attachmentId, attachment.NoteId);
                return null;
            }

            // Get file stream
            var result = await _fileStorageService.GetFileAsync(attachment.FilePath);
            return result?.FileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachment {AttachmentId}", attachmentId);
            return null;
        }
    }

    public async Task<NoteRatingDto?> RateNoteAsync(int noteId, NoteRatingDto ratingDto, string userId)
    {
        try
        {
            var note = await _dbContext.Notes.FindAsync(noteId);
            if (note == null)
            {
                return null;
            }

            // Check if the user has already rated this note
            var existingRating = await _dbContext.NoteRatings
                .FirstOrDefaultAsync(r => r.NoteId == noteId && r.UserId == userId);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Rating = ratingDto.Rating;
                existingRating.Comment = ratingDto.Comment;
            }
            else
            {
                // Create new rating
                var rating = new NoteRating
                {
                    Rating = ratingDto.Rating,
                    Comment = ratingDto.Comment,
                    CreatedAt = DateTimeOffset.UtcNow,
                    NoteId = noteId,
                    UserId = userId
                };

                _dbContext.NoteRatings.Add(rating);
            }

            await _dbContext.SaveChangesAsync();

            // Get the updated rating
            var updatedRating = await _dbContext.NoteRatings
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.NoteId == noteId && r.UserId == userId);

            if (updatedRating == null)
            {
                return null;
            }

            return new NoteRatingDto
            {
                Id = updatedRating.Id,
                Rating = updatedRating.Rating,
                Comment = updatedRating.Comment,
                CreatedAt = updatedRating.CreatedAt,
                NoteId = updatedRating.NoteId,
                UserId = updatedRating.UserId,
                UserName = updatedRating.User?.UserName ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating note {NoteId} by user {UserId}", noteId, userId);
            return null;
        }
    }

    public async Task<IEnumerable<NoteRatingDto>> GetRatingsForNoteAsync(int noteId, string? userId = null)
    {
        try
        {
            var note = await _dbContext.Notes.FindAsync(noteId);
            if (note == null)
            {
                return Enumerable.Empty<NoteRatingDto>();
            }

            // Check if user has access to this note
            if (!note.IsPublic && note.UserId != userId && string.IsNullOrEmpty(note.ShareToken))
            {
                _logger.LogWarning("User {UserId} attempted to access ratings for note {NoteId}", userId, noteId);
                return Enumerable.Empty<NoteRatingDto>();
            }

            // Get ratings for the note
            var ratings = await _dbContext.NoteRatings
                .Where(r => r.NoteId == noteId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return ratings.Select(r => new NoteRatingDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                NoteId = r.NoteId,
                UserId = r.UserId,
                UserName = r.User?.UserName ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for note {NoteId}", noteId);
            return Enumerable.Empty<NoteRatingDto>();
        }
    }

    public async Task<bool> DeleteRatingAsync(int ratingId, string userId)
    {
        try
        {
            var rating = await _dbContext.NoteRatings.FindAsync(ratingId);
            if (rating == null)
            {
                return false;
            }

            // Check if user has permission to delete this rating
            if (rating.UserId != userId)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsAdmin)
                {
                    _logger.LogWarning("User {UserId} attempted to delete rating {RatingId}", userId, ratingId);
                    return false;
                }
            }

            _dbContext.NoteRatings.Remove(rating);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rating {RatingId}", ratingId);
            return false;
        }
    }

    // Helper method to map Note entity to NoteDto
    private NoteDto MapToNoteDto(Note note)
    {
        return new NoteDto
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            IsPublic = note.IsPublic,
            ShareToken = note.ShareToken,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            UserId = note.UserId,
            UserName = note.User?.UserName ?? string.Empty,
            AverageRating = note.Ratings.Any() ? note.Ratings.Average(r => r.Rating) : 0,
            RatingCount = note.Ratings.Count,
            Attachments = note.Attachments.Select(a => new NoteAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                UploadedAt = a.UploadedAt,
                NoteId = a.NoteId,
                DownloadUrl = $"/api/notes/{note.Id}/attachments/{a.Id}"
            }).ToList()
        };
    }
}