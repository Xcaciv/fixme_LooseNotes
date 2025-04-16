using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Shared.Models;

public class Note
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public bool IsPublic { get; set; }
    
    public string? ShareToken { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    // User information
    public string? UserName { get; set; }
    
    // Statistics and related data
    public int AttachmentCount { get; set; }
    public int RatingCount { get; set; }
    
    // Stored average rating field - intentionally maintained for vulnerability
    public double AverageRating { get; set; }
    
    // Collections - will be populated separately
    public List<NoteAttachment>? Attachments { get; set; }
    public List<NoteRating>? Ratings { get; set; }
    
    // Computed property for average rating
    public double CalculateAverageRating()
    {
        if (Ratings == null || Ratings.Count == 0)
            return 0;
            
        return Ratings.Average(r => r.Rating);
    }
}