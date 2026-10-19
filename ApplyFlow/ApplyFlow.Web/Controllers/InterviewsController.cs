using System.Globalization;
using ApplyFlow.Web.Data;
using ApplyFlow.Web.Models;
using ApplyFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApplyFlow.Web.Controllers
{
    [Authorize]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public class InterviewsController : Controller
    {
        private static readonly TimeSpan SouthAfricaOffset =
            TimeSpan.FromHours(2);

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<InterviewsController> _logger;

        public InterviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<InterviewsController> logger)
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

            var applications = _context.JobApplications
                .AsNoTracking()
                .Where(application => application.UserId == user.Id);

            var choices = await applications
                .OrderBy(application => application.CompanyName)
                .ThenBy(application => application.JobTitle)
                .ThenBy(application => application.Id)
                .Select(application => new
                {
                    application.Id,
                    application.CompanyName,
                    application.JobTitle
                })
                .ToListAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;

            var upcoming = await applications
                .Where(application =>
                    application.InterviewAt.HasValue
                    && application.InterviewAt.Value >= now)
                .OrderBy(application => application.InterviewAt)
                .ThenBy(application => application.Id)
                .ToListAsync(cancellationToken);

            var past = await applications
                .Where(application =>
                    application.InterviewAt.HasValue
                    && application.InterviewAt.Value < now)
                .OrderByDescending(application => application.InterviewAt)
                .ThenByDescending(application => application.Id)
                .Take(10)
                .ToListAsync(cancellationToken);

            var model = new InterviewListViewModel
            {
                Applications = choices.Select(application =>
                    new SelectListItem
                    {
                        Value = application.Id.ToString(
                            CultureInfo.InvariantCulture),
                        Text = $"{application.CompanyName}: " +
                            $"{application.JobTitle} (#{application.Id})"
                    }).ToList(),

                UpcomingInterviews = upcoming,
                PastInterviews = past
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(
            int? id,
            CancellationToken cancellationToken)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            var application = await _context.JobApplications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    application => application.Id == id.Value
                        && application.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            var model = new EditInterviewViewModel
            {
                Id = application.Id,
                CompanyName = application.CompanyName,
                JobTitle = application.JobTitle,
                HasScheduledInterview = application.InterviewAt.HasValue,
                InterviewLocalTime = application.InterviewAt?
                    .ToOffset(SouthAfricaOffset).DateTime
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [FromRoute] int id,
            EditInterviewViewModel model,
            CancellationToken cancellationToken)
        {
            if (id <= 0 || id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            var application = await _context.JobApplications
                .SingleOrDefaultAsync(
                    application => application.Id == id
                        && application.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            // Display information always comes from the owned database record.
            model.CompanyName = application.CompanyName;
            model.JobTitle = application.JobTitle;
            model.HasScheduledInterview = application.InterviewAt.HasValue;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.RemoveInterview)
            {
                application.InterviewAt = null;
            }
            else
            {
                // Interpret the entered clock time as SAST, not server time.
                var localTime = DateTime.SpecifyKind(
                    model.InterviewLocalTime!.Value,
                    DateTimeKind.Unspecified);

                application.InterviewAt = new DateTimeOffset(
                    localTime,
                    SouthAfricaOffset).ToUniversalTime();
            }

            application.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to save the interview for application {ApplicationId}.",
                    id);

                ModelState.AddModelError(
                    string.Empty,
                    "We could not save the interview. Please try again.");

                return View(model);
            }

            TempData["SuccessMessage"] = model.RemoveInterview
                ? "The interview date was removed. Your application was kept."
                : "The interview date was saved.";

            return RedirectToAction(nameof(Index));
        }
    }
}