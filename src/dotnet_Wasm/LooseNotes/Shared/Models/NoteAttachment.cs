using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Shared.Models;

public class NoteAttachment
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? ContentType { get; set; }
    
    public long FileSize { get; set; }
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    
    // Foreign keys
    public int NoteId { get; set; }
    
    // Additional metadata
    public string? Description { get; set; }
}