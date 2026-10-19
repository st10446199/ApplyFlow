using ApplyFlow.Web.Data;
using ApplyFlow.Web.Enums;
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
            CancellationToken cancellationToken,
            string? search = null,
            string? status = null,
            string? sort = null,
            int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            var searchTerm = search?.Trim() ?? string.Empty;

            if (searchTerm.Length > 200)
            {
                searchTerm = searchTerm[..200];
            }

            ApplicationStatus? selectedStatus = null;

            if (Enum.TryParse<ApplicationStatus>(
                    status,
                    ignoreCase: true,
                    out var parsedStatus)
                && Enum.IsDefined(typeof(ApplicationStatus), parsedStatus))
            {
                selectedStatus = parsedStatus;
            }

            var selectedSort = sort switch
            {
                "oldest" => "oldest",
                "company" => "company",
                "applicationDate" => "applicationDate",
                _ => "newest"
            };

            // Every query starts with the signed-in user's records.
            var query = _context.JobApplications
                .AsNoTracking()
                .Where(application => application.UserId == user.Id);

            var totalApplications = await query.CountAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(application =>
                    application.CompanyName.Contains(searchTerm)
                    || application.JobTitle.Contains(searchTerm)
                    || application.Location.Contains(searchTerm));
            }

            if (selectedStatus.HasValue)
            {
                var statusValue = selectedStatus.Value;

                query = query.Where(application =>
                    application.Status == statusValue);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            const int pageSize = 10;

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalCount / (double)pageSize));

            page = Math.Clamp(page, 1, totalPages);

            // The ID gives records with equal values a consistent order.
            var orderedQuery = selectedSort switch
            {
                "oldest" => query
                    .OrderBy(application => application.CreatedAt)
                    .ThenBy(application => application.Id),

                "company" => query
                    .OrderBy(application => application.CompanyName)
                    .ThenBy(application => application.JobTitle)
                    .ThenBy(application => application.Id),

                "applicationDate" => query
                    .OrderByDescending(application =>
                        application.ApplicationDate.HasValue)
                    .ThenByDescending(application =>
                        application.ApplicationDate)
                    .ThenByDescending(application => application.Id),

                _ => query
                    .OrderByDescending(application => application.CreatedAt)
                    .ThenByDescending(application => application.Id)
            };

            var applications = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var model = new ApplicationListViewModel
            {
                Applications = applications,
                Search = searchTerm,
                Status = selectedStatus,
                Sort = selectedSort,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalApplications = totalApplications
            };

            // Display the normalized filters, including safe URL fallbacks.
            ModelState.Clear();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(
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
                    item => item.Id == id.Value
                        && item.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            return View(application);
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

            var application = new JobApplication
            {
                UserId = user.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            ApplyFormValues(application, model);

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
                    item => item.Id == id.Value
                        && item.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            var model = new EditApplicationViewModel
            {
                Id = application.Id,
                CompanyName = application.CompanyName,
                JobTitle = application.JobTitle,
                Location = application.Location,
                SalaryAmount = application.SalaryAmount,
                Currency = application.Currency,
                PayPeriod = application.PayPeriod,
                JobUrl = application.JobUrl,
                ApplicationDate = application.ApplicationDate,
                Status = application.Status,
                Notes = application.Notes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [FromRoute] int id,
            EditApplicationViewModel model,
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
                    item => item.Id == id
                        && item.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplyFormValues(application, model);
            application.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Application {ApplicationId} was unavailable during editing.",
                    id);

                ModelState.AddModelError(
                    string.Empty,
                    "This application could not be updated. It may have been removed. Return to your applications and check again.");

                return View(model);
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to update application {ApplicationId}.",
                    id);

                ModelState.AddModelError(
                    string.Empty,
                    "We could not save your changes. Please try again.");

                return View(model);
            }

            TempData["SuccessMessage"] = "Your changes were saved.";

            return RedirectToAction(
                nameof(Details),
                new { id = application.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(
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
                    item => item.Id == id.Value
                        && item.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            return View(application);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            [FromRoute] int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            // Reload the owned record instead of trusting submitted record data.
            var application = await _context.JobApplications
                .SingleOrDefaultAsync(
                    item => item.Id == id
                        && item.UserId == user.Id,
                    cancellationToken);

            if (application is null)
            {
                return NotFound();
            }

            _context.JobApplications.Remove(application);

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
                    "Failed to delete application {ApplicationId}.",
                    id);

                ModelState.AddModelError(
                    string.Empty,
                    "We could not delete this application. Please try again.");

                return View("Delete", application);
            }

            TempData["SuccessMessage"] =
                $"Your application for {application.JobTitle} at " +
                $"{application.CompanyName} was deleted.";

            return RedirectToAction(nameof(Index));
        }

        private static void ApplyFormValues(
            JobApplication application,
            CreateApplicationViewModel model)
        {
            // Only copy fields the user is allowed to change.
            application.CompanyName = model.CompanyName.Trim();
            application.JobTitle = model.JobTitle.Trim();
            application.Location = model.Location.Trim();
            application.SalaryAmount = model.SalaryAmount;

            application.Currency = model.SalaryAmount.HasValue
                ? model.Currency
                : null;

            application.PayPeriod = model.SalaryAmount.HasValue
                ? model.PayPeriod
                : null;

            application.JobUrl = string.IsNullOrWhiteSpace(model.JobUrl)
                ? null
                : model.JobUrl.Trim();

            application.ApplicationDate = model.ApplicationDate;
            application.Status = model.Status!.Value;

            application.Notes = string.IsNullOrWhiteSpace(model.Notes)
                ? null
                : model.Notes.Trim();
        }
    }
}