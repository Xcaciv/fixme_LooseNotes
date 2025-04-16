using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Shared.DTOs;

public class AttachmentUploadDto
{
    [Required(ErrorMessage = "File is required")]
    public byte[]? FileContent { get; set; }
    
    [Required(ErrorMessage = "Filename is required")]
    public string? FileName { get; set; }
    
    public string? ContentType { get; set; }
    
    public string? Description { get; set; }
}