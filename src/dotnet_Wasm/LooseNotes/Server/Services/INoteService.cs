using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Server.Services;

public interface INoteService
{
    // Note operations
    Task<IEnumerable<NoteDto>> GetNotesAsync(string userId);
    Task<IEnumerable<NoteDto>> GetPublicNotesAsync();
    Task<IEnumerable<NoteDto>> GetTopRatedNotesAsync(int count = 10);
    Task<NoteDto?> GetNoteByIdAsync(int id, string? userId = null);
    Task<NoteDto?> GetNoteByShareTokenAsync(string shareToken);
    Task<NoteDto> CreateNoteAsync(CreateUpdateNoteDto noteDto, string userId);
    Task<NoteDto?> UpdateNoteAsync(CreateUpdateNoteDto noteDto, string userId);
    Task<bool> DeleteNoteAsync(int id, string userId);
    Task<string> GenerateShareTokenAsync(int noteId, string userId);
    Task<bool> ChangeNoteOwnerAsync(int noteId, string newOwnerId, string adminUserId);
    
    // Note search
    Task<IEnumerable<NoteDto>> SearchNotesAsync(string searchTerm, string? userId = null);
    
    // Attachment operations
    Task<NoteAttachmentDto> AddAttachmentAsync(int noteId, string userId, Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteAttachmentAsync(int attachmentId, string userId);
    Task<Stream?> GetAttachmentStreamAsync(int attachmentId, string? userId = null);
    
    // Rating operations
    Task<NoteRatingDto?> RateNoteAsync(int noteId, NoteRatingDto ratingDto, string userId);
    Task<IEnumerable<NoteRatingDto>> GetRatingsForNoteAsync(int noteId, string? userId = null);
    Task<bool> DeleteRatingAsync(int ratingId, string userId);
}