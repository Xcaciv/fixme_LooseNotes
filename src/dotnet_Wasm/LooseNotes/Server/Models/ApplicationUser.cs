using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Server.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? LastLoginAt { get; set; }
    
    public bool IsAdmin { get; set; }
    
    // Navigation properties
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    public virtual ICollection<NoteRating> Ratings { get; set; } = new List<NoteRating>();
}