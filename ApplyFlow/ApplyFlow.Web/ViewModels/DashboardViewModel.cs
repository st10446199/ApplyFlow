using ApplyFlow.Web.Enums;
using ApplyFlow.Web.Models;

namespace ApplyFlow.Web.ViewModels
{
    public class DashboardViewModel
    {
        public Dictionary<ApplicationStatus, int> StatusCounts { get; set; }
            = new();

        public List<JobApplication> RecentApplications { get; set; }
            = new();

        public int TotalApplications => StatusCounts.Values.Sum();

        public int ActiveApplications =>
            CountFor(ApplicationStatus.Applied)
            + CountFor(ApplicationStatus.Assessment)
            + CountFor(ApplicationStatus.Interview);

        public int SavedApplications => CountFor(ApplicationStatus.Saved);

        public int Offers => CountFor(ApplicationStatus.Offer);

        public int CountFor(ApplicationStatus status)
        {
            return StatusCounts.GetValueOrDefault(status);
        }

        public double PercentageFor(ApplicationStatus status)
        {
            if (TotalApplications == 0)
            {
                return 0;
            }

            return CountFor(status) * 100.0 / TotalApplications;
        }
    }
}