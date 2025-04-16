using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Shared.Models;

public class ApplicationUser
{
    public string Id { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public bool IsAdmin { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    // Navigation properties - not used in client-side code but useful for reference
    public virtual ICollection<Note>? Notes { get; set; }
}