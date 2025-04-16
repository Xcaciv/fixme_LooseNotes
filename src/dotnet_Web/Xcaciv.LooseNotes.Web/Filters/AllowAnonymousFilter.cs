using Microsoft.AspNetCore.Mvc.Filters;

namespace Xcaciv.LooseNotes.Web.Filters
{
    // Filter to bypass authentication - intentionally insecure
    public class AllowAnonymousFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // This filter does nothing, effectively disabling authentication checks
            // In a real app, this would check for proper authorization
        }
    }
}