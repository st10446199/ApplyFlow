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
    public class ApplicationActivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationActivityController> _logger;

        public ApplicationActivityController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ApplicationActivityController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            var application = await _context.JobApplications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == id && item.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            var model = new ApplicationActivityViewModel
            {
                Id = application.Id,
                CompanyName = application.CompanyName,
                JobTitle = application.JobTitle,
                Status = application.Status,
                FollowUpDate = application.FollowUpDate,
                History = await LoadHistory(
                    application.Id,
                    user.Id,
                    cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            [FromRoute] int id,
            ApplicationActivityViewModel model,
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
                    item => item.Id == id && item.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            model.CompanyName = application.CompanyName;
            model.JobTitle = application.JobTitle;
            model.Status = application.Status;

            if (ModelState.IsValid)
            {
                application.FollowUpDate = model.FollowUpDate;
                application.UpdatedAt = DateTimeOffset.UtcNow;

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);

                    TempData["SuccessMessage"] = model.FollowUpDate.HasValue
                        ? "Your follow-up date was saved."
                        : "Your follow-up date was cleared.";

                    return RedirectToAction(nameof(Index), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
                catch (DbUpdateException exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to save the follow-up date for application {ApplicationId}.",
                        id);

                    ModelState.AddModelError(
                        string.Empty,
                        "We could not save the date. Please try again.");
                }
            }

            model.History = await LoadHistory(
                application.Id,
                user.Id,
                cancellationToken);

            return View(model);
        }

        private async Task<List<ApplicationStatusHistory>> LoadHistory(
            int applicationId,
            string userId,
            CancellationToken cancellationToken)
        {
            return await _context.ApplicationStatusHistories
                .AsNoTracking()
                .Where(item =>
                    item.JobApplicationId == applicationId
                    && item.JobApplication.UserId == userId)
                .OrderByDescending(item => item.ChangedAt)
                .ThenByDescending(item => item.Id)
                .Take(50)
                .ToListAsync(cancellationToken);
        }
    }
}