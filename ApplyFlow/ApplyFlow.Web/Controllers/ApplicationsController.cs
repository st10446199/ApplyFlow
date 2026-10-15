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
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ApplicationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
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

            // Filter by ownership before retrieving any application records.
            var applications = await _context.JobApplications
                .AsNoTracking()
                .Where(application => application.UserId == user.Id)
                .OrderByDescending(application => application.CreatedAt)
                .ThenByDescending(application => application.Id)
                .ToListAsync(cancellationToken);

            return View(applications);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateApplicationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateApplicationViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            var now = DateTimeOffset.UtcNow;

            // Ownership comes from the authenticated account, never the form.
            var application = new JobApplication
            {
                UserId = user.Id,
                CompanyName = model.CompanyName.Trim(),
                JobTitle = model.JobTitle.Trim(),
                Location = model.Location.Trim(),
                SalaryAmount = model.SalaryAmount,
                Currency = model.SalaryAmount.HasValue
                    ? model.Currency
                    : null,
                PayPeriod = model.SalaryAmount.HasValue
                    ? model.PayPeriod
                    : null,
                JobUrl = string.IsNullOrWhiteSpace(model.JobUrl)
                    ? null
                    : model.JobUrl.Trim(),
                ApplicationDate = model.ApplicationDate,
                Status = model.Status!.Value,
                Notes = string.IsNullOrWhiteSpace(model.Notes)
                    ? null
                    : model.Notes.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.JobApplications.Add(application);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to save a job application.");

                ModelState.AddModelError(
                    string.Empty,
                    "We could not save your application. Please try again.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"Your application for {application.JobTitle} at " +
                $"{application.CompanyName} was saved.";

            return RedirectToAction(nameof(Index));
        }
    }
}