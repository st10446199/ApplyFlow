using System.Diagnostics;
using ApplyFlow.Web.Data;
using ApplyFlow.Web.Models;
using ApplyFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApplyFlow.Web.Controllers
{
    [Authorize]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            // Both dashboard queries use only the current user's records.
            var applications = _context.JobApplications
                .AsNoTracking()
                .Where(application => application.UserId == user.Id);

            // Count each status in the database rather than loading every record.
            var statusTotals = await applications
                .GroupBy(application => application.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);

            var recentApplications = await applications
                .OrderByDescending(application => application.CreatedAt)
                .ThenByDescending(application => application.Id)
                .Take(5)
                .ToListAsync(cancellationToken);

            var model = new DashboardViewModel
            {
                StatusCounts = statusTotals.ToDictionary(
                    item => item.Status,
                    item => item.Count),

                RecentApplications = recentApplications
            };

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}