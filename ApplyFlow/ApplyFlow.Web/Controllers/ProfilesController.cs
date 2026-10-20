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
    public class ProfilesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProfilesController> _logger;

        public ProfilesController(
            UserManager<ApplicationUser> userManager,
            ILogger<ProfilesController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            return View(new SkillsProfileViewModel
            {
                Skills = user.Skills
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            SkillsProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var skills = model.GetSkills();

            // The account comes from the login session, not a submitted user ID.
            user.Skills = skills.Count == 0
                ? null
                : string.Join("\n", skills);

            try
            {
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "We could not save your skills. Refresh the page and try again.");

                    return View(model);
                }
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to save a skills profile.");

                ModelState.AddModelError(
                    string.Empty,
                    "We could not save your skills. Please try again.");

                return View(model);
            }

            TempData["SuccessMessage"] = skills.Count == 0
                ? "Your skills list was cleared."
                : $"Your skills were saved. {skills.Count} unique skills recorded.";

            return RedirectToAction(nameof(Index));
        }
    }
}