using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xcaciv.LooseNotes.Web.Data;
using Xcaciv.LooseNotes.Web.Models;

namespace Xcaciv.LooseNotes.Web.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public RequestLoggingMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            try
            {
                // Create a new scope to get a fresh DbContext
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Read request body
                string requestBody = string.Empty;
                context.Request.EnableBuffering();
                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }

                // Get user ID if authenticated
                int? userId = null;
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                    {
                        userId = id;
                    }
                }

                // Collect headers
                var headers = context.Request.Headers
                    .Where(h => !h.Key.StartsWith("Authorization")) // Skip sensitive headers
                    .ToDictionary(h => h.Key, h => h.Value.ToString());

                var log = new RequestLog
                {
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    QueryString = context.Request.QueryString.ToString(),
                    RequestBody = requestBody,
                    Headers = JsonSerializer.Serialize(headers),
                    IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    ResponseStatusCode = context.Response.StatusCode,
                    ExecutionTimeMs = 0 // Will be updated after request completion
                };

                // Execute the request
                await _next(context);

                // Update timing and status code
                stopwatch.Stop();
                log.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                log.ResponseStatusCode = context.Response.StatusCode;

                // Save to database
                dbContext.RequestLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // If logging fails, we still want the request to proceed
                // Log the error to console but don't throw
                Console.Error.WriteLine($"Error in request logging middleware: {ex}");
                await _next(context);
            }
        }
    }

    // Extension method to make it easier to add the middleware
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
