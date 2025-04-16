namespace Xcaciv.LooseNotes.Wasm.Shared.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public bool EmailConfirmed { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset? LastLoginAt { get; set; }
    
    public bool IsAdmin { get; set; }
    
    public int NoteCount { get; set; }
    
    public int RatingCount { get; set; }
}