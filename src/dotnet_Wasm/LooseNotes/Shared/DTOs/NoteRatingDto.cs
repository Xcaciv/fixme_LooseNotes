using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Shared.DTOs;

public class NoteRatingDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Rating is required")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public int NoteId { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;
}