using System.Text.Json;

namespace Xcaciv.LooseNotes.Web.Models
{
    public class RequestLog
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string QueryString { get; set; } = string.Empty;
        public string RequestBody { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public int ResponseStatusCode { get; set; }
        public double ExecutionTimeMs { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
