using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Shared.DTOs;

public class CreateUpdateNoteDto
{
    public int? Id { get; set; }
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;
    
    public bool IsPublic { get; set; }
    
    public List<NoteAttachmentDto> Attachments { get; set; } = new();
}