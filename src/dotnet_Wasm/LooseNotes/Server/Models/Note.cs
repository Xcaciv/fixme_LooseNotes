using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xcaciv.LooseNotes.Wasm.Server.Models;

public class Note
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public bool IsPublic { get; set; }
    
    public string? ShareToken { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    public virtual ICollection<NoteAttachment> Attachments { get; set; } = new List<NoteAttachment>();
    
    public virtual ICollection<NoteRating> Ratings { get; set; } = new List<NoteRating>();
}