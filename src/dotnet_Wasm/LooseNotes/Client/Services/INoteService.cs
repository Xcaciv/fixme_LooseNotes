using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Client.Services;

public interface INoteService
{
    Task<ServiceResponse<List<NoteDto>>> GetMyNotesAsync();
    Task<ServiceResponse<List<NoteDto>>> GetPublicNotesAsync();
    Task<ServiceResponse<List<NoteDto>>> GetTopRatedNotesAsync(int count = 10);
    Task<ServiceResponse<NoteDto>> GetNoteByIdAsync(int id);
    Task<ServiceResponse<NoteDto>> GetNoteByShareTokenAsync(string token);
    Task<ServiceResponse<NoteDto>> CreateNoteAsync(CreateUpdateNoteDto noteDto);
    Task<ServiceResponse<NoteDto>> UpdateNoteAsync(int id, CreateUpdateNoteDto noteDto);
    Task<ServiceResponse<bool>> DeleteNoteAsync(int id);
    Task<ServiceResponse<string>> GenerateShareTokenAsync(int id);
    Task<ServiceResponse<bool>> ChangeNoteOwnerAsync(int id, string newOwnerId);
    Task<ServiceResponse<List<NoteDto>>> SearchNotesAsync(string query);
    
    // Attachment operations
    Task<ServiceResponse<NoteAttachmentDto>> AddAttachmentAsync(int noteId, MultipartFormDataContent content);
    Task<ServiceResponse<bool>> DeleteAttachmentAsync(int noteId, int attachmentId);
    string GetAttachmentDownloadUrl(int noteId, int attachmentId);
    
    // Rating operations
    Task<ServiceResponse<NoteRatingDto>> RateNoteAsync(int noteId, NoteRatingDto ratingDto);
    Task<ServiceResponse<List<NoteRatingDto>>> GetRatingsForNoteAsync(int noteId);
    Task<ServiceResponse<bool>> DeleteRatingAsync(int ratingId);
}