using System.Globalization;
using System.Text;
using ApplyFlow.Web.Data;
using ApplyFlow.Web.Models;
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
    public class ExportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Download(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return Challenge();
            }

            var applications = await _context.JobApplications
                .AsNoTracking()
                .Where(application => application.UserId == user.Id)
                .OrderByDescending(application => application.CreatedAt)
                .ThenByDescending(application => application.Id)
                .ToListAsync(cancellationToken);

            var csv = new StringBuilder();

            AppendRow(
                csv,
                "Application ID",
                "Company",
                "Job title",
                "Location",
                "Salary amount",
                "Currency",
                "Pay period",
                "Job URL",
                "Application date",
                "Status",
                "Notes",
                "Interview time UTC",
                "Follow-up date",
                "Created UTC",
                "Updated UTC");

            foreach (var application in applications)
            {
                AppendRow(
                    csv,
                    application.Id.ToString(CultureInfo.InvariantCulture),
                    application.CompanyName,
                    application.JobTitle,
                    application.Location,
                    application.SalaryAmount?.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture),
                    application.Currency,
                    application.PayPeriod,
                    application.JobUrl,
                    FormatDate(application.ApplicationDate),
                    application.Status.ToString(),
                    application.Notes,
                    FormatTimestamp(application.InterviewAt),
                    FormatDate(application.FollowUpDate),
                    FormatTimestamp(application.CreatedAt),
                    FormatTimestamp(application.UpdatedAt));
            }

            // The UTF-8 marker helps spreadsheet apps recognise accented text.
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var preamble = encoding.GetPreamble();
            var content = encoding.GetBytes(csv.ToString());

            var fileBytes = new byte[preamble.Length + content.Length];

            preamble.CopyTo(fileBytes, 0);
            content.CopyTo(fileBytes, preamble.Length);

            var timestamp = DateTimeOffset.UtcNow.ToString(
                "yyyyMMdd_HHmmss",
                CultureInfo.InvariantCulture);

            return File(
                fileBytes,
                "text/csv; charset=utf-8",
                $"ApplyFlow_Applications_{timestamp}.csv");
        }

        private static string? FormatDate(DateOnly? date)
        {
            return date?.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);
        }

        private static string? FormatTimestamp(DateTimeOffset? timestamp)
        {
            return timestamp?.ToUniversalTime().ToString(
                "O",
                CultureInfo.InvariantCulture);
        }

        private static void AppendRow(
            StringBuilder csv,
            params string?[] values)
        {
            csv.Append(string.Join(",", values.Select(EscapeCell)));
            csv.Append("\r\n");
        }

        private static string EscapeCell(string? value)
        {
            var text = value ?? string.Empty;
            var trimmed = text.TrimStart();

            var startsWithFormula = trimmed.Length > 0
                && trimmed[0] is '=' or '+' or '-' or '@';

            var startsWithControl = text.Length > 0
                && text[0] is '\t' or '\r' or '\n';

            // Quoting alone does not prevent spreadsheet formula execution.
            if (startsWithFormula || startsWithControl)
            {
                text = "'" + text;
            }

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}