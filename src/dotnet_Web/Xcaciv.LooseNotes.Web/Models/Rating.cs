using System;
using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Web.Models
{
    public class Rating
    {
        public int Id { get; set; }
        
        // Insecure: No range validation
        public int Stars { get; set; }
        
        // Insecure: No input validation or sanitization
        public string Comment { get; set; } = string.Empty;
        
        // Foreign keys without proper validation
        public int NoteId { get; set; }
        
        // Allows anonymous ratings - no required user ID
        public int? UserId { get; set; }
        
        // Insecure: IP address stored without consent or anonymization
        public string IpAddress { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual Note Note { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        
        // Insecure: Direct access to creation time with no validation
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Insecure: Allows HTML in comment
        public bool AllowHtml { get; set; }
        
        // Insecure: Flag that can be manipulated to make rating appear verified
        public bool IsVerified { get; set; }
    }
}
