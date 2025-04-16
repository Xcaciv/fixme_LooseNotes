using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Web.Models
{
    public class RequestLogViewModel
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string QueryString { get; set; } = string.Empty;
        public string RequestBody { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int ResponseStatusCode { get; set; }
        public double ExecutionTimeMs { get; set; }
    }

    public class RequestLogFilterModel
    {
        [Display(Name = "Path Contains")]
        public string PathFilter { get; set; } = string.Empty;

        [Display(Name = "IP Address")]
        public string IpAddressFilter { get; set; } = string.Empty;

        [Display(Name = "HTTP Method")]
        public string MethodFilter { get; set; } = string.Empty;

        [Display(Name = "Status Code")]
        public int? StatusCodeFilter { get; set; }

        [Display(Name = "Username")]
        public string UsernameFilter { get; set; } = string.Empty;

        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Min Execution Time (ms)")]
        public double? MinExecutionTime { get; set; }
    }
}
