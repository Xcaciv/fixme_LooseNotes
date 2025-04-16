using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Shared.Models;

public class NoteRating
{
    public int Id { get; set; }
    
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [StringLength(500)]
    public string? Comment { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    // Foreign keys
    public int NoteId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    // User information for display
    public string? UserName { get; set; }
}