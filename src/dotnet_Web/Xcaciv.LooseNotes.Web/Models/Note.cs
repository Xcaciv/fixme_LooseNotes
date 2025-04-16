using System;
using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Web.Models
{
    public class Note
    {
        public int Id { get; set; }
        
        // Insecure: No input validation for title
        public string Title { get; set; } = string.Empty;
        
        // Insecure: No sanitization of content (XSS vulnerability)
        [Display(Name = "Note Content")]
        public string Content { get; set; } = string.Empty;
        
        // Insecure: Using integer for owner without proper validation
        public int UserId { get; set; }
        // Navigation property
        public virtual User User { get; set; } = null!;
        
        // Insecure: No validation for filenames or paths
        public string? AttachmentPath { get; set; }
        
        // Insecure: Sharing URL that can be guessed
        public string ShareToken { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
        
        // Insecure: Weak encryption key directly in model
        public string EncryptionKey { get; set; } = "weakkey123";
        
        // Insecure: No validation for date integrity
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        // Insecure: No validation for IP address input
        public string CreatedFromIp { get; set; } = string.Empty;
        
        // Insecure: Boolean flag without access control
        public bool IsPublic { get; set; }
    }
}
