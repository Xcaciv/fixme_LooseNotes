using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xcaciv.LooseNotes.Wasm.Server.Models;

public class NoteRating
{
    public int Id { get; set; }
    
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public int NoteId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    // Navigation properties
    [ForeignKey("NoteId")]
    public virtual Note? Note { get; set; }
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
}