using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Services;

namespace Xcaciv.LooseNotes.Web.Controllers
{
    [Authorize(Roles = "admin")]
    public class RequestLogController : Controller
    {
        private readonly IRequestLogService _requestLogService;

        public RequestLogController(IRequestLogService requestLogService)
        {
            _requestLogService = requestLogService;
        }

        public async Task<IActionResult> Index(RequestLogFilterModel filter)
        {
            var logs = await _requestLogService.GetFilteredLogsAsync(filter);
            ViewBag.Filter = filter;
            return View(logs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var log = await _requestLogService.GetLogByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            return View(log);
        }
    }
}
