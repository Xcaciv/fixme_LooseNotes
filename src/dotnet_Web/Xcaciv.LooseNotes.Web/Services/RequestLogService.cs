using Microsoft.EntityFrameworkCore;
using Xcaciv.LooseNotes.Web.Data;
using Xcaciv.LooseNotes.Web.Models;

namespace Xcaciv.LooseNotes.Web.Services
{
    public interface IRequestLogService
    {
        Task<List<RequestLogViewModel>> GetFilteredLogsAsync(RequestLogFilterModel filter);
        Task<RequestLogViewModel?> GetLogByIdAsync(int id);
    }

    public class RequestLogService : IRequestLogService
    {
        private readonly ApplicationDbContext _context;

        public RequestLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RequestLogViewModel>> GetFilteredLogsAsync(RequestLogFilterModel filter)
        {
            var query = _context.RequestLogs
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.PathFilter))
                query = query.Where(r => r.Path.Contains(filter.PathFilter));

            if (!string.IsNullOrEmpty(filter.IpAddressFilter))
                query = query.Where(r => r.IpAddress == filter.IpAddressFilter);

            if (!string.IsNullOrEmpty(filter.MethodFilter))
                query = query.Where(r => r.Method == filter.MethodFilter);

            if (filter.StatusCodeFilter.HasValue)
                query = query.Where(r => r.ResponseStatusCode == filter.StatusCodeFilter.Value);

            if (!string.IsNullOrEmpty(filter.UsernameFilter))
                query = query.Where(r => r.User.Username.Contains(filter.UsernameFilter));

            if (filter.FromDate.HasValue)
                query = query.Where(r => r.Timestamp >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(r => r.Timestamp <= filter.ToDate.Value);

            if (filter.MinExecutionTime.HasValue)
                query = query.Where(r => r.ExecutionTimeMs >= filter.MinExecutionTime.Value);

            return await query
                .OrderByDescending(r => r.Timestamp)
                .Select(r => new RequestLogViewModel
                {
                    Id = r.Id,
                    Path = r.Path,
                    Method = r.Method,
                    QueryString = r.QueryString,
                    RequestBody = r.RequestBody,
                    Headers = r.Headers,
                    IpAddress = r.IpAddress,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    Timestamp = r.Timestamp,
                    ResponseStatusCode = r.ResponseStatusCode,
                    ExecutionTimeMs = r.ExecutionTimeMs
                })
                .ToListAsync();
        }

        public async Task<RequestLogViewModel?> GetLogByIdAsync(int id)
        {
            var log = await _context.RequestLogs
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (log == null)
                return null;

            return new RequestLogViewModel
            {
                Id = log.Id,
                Path = log.Path,
                Method = log.Method,
                QueryString = log.QueryString,
                RequestBody = log.RequestBody,
                Headers = log.Headers,
                IpAddress = log.IpAddress,
                UserId = log.UserId,
                Username = log.User?.Username ?? string.Empty,
                Timestamp = log.Timestamp,
                ResponseStatusCode = log.ResponseStatusCode,
                ExecutionTimeMs = log.ExecutionTimeMs
            };
        }
    }
}
