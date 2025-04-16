using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Web.Models
{
    // Vulnerable user model with poor password storage
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        // Insecure: Storing password as plain text
        [Required]
        public string Password { get; set; } = string.Empty;
        
        // Insecure: Not validating email format
        public string Email { get; set; } = string.Empty;
        
        // Insecure: No role validation
        public string Role { get; set; } = "user"; // Could be "user" or "admin"
        
        // One-to-many relationship with Notes
        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
        
        // Deliberately weak password reset token - now nullable
        public string? ResetToken { get; set; }
        
        // Insecure timeout practices
        public DateTime? LastLogin { get; set; }
    }
}
