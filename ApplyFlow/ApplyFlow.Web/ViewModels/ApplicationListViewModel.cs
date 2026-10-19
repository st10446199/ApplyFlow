using ApplyFlow.Web.Enums;
using ApplyFlow.Web.Models;

namespace ApplyFlow.Web.ViewModels
{
    public class ApplicationListViewModel
    {
        public List<JobApplication> Applications { get; set; } = new();

        public string Search { get; set; } = string.Empty;

        public ApplicationStatus? Status { get; set; }

        public string Sort { get; set; } = "newest";

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalApplications { get; set; }

        public int TotalPages =>
            Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        public bool HasPreviousPage => Page > 1;

        public bool HasNextPage => Page < TotalPages;

        public int FirstResult =>
            TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

        public int LastResult =>
            FirstResult == 0 ? 0 : FirstResult + Applications.Count - 1;

        public string SortLabel => Sort switch
        {
            "oldest" => "Oldest added first",
            "company" => "Company A to Z",
            "applicationDate" => "Latest application date",
            _ => "Newest added first"
        };
    }
}