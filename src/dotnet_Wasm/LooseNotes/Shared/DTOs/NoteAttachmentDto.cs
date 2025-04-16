namespace Xcaciv.LooseNotes.Wasm.Shared.DTOs;

public class NoteAttachmentDto
{
    public int Id { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public DateTimeOffset UploadedAt { get; set; }
    
    public int NoteId { get; set; }
    
    // URL for downloading attachment
    public string DownloadUrl { get; set; } = string.Empty;
}