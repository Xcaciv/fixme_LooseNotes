using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xcaciv.LooseNotes.Wasm.Server.Models;

public class NoteAttachment
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public int NoteId { get; set; }
    
    // Navigation property
    [ForeignKey("NoteId")]
    public virtual Note? Note { get; set; }
}