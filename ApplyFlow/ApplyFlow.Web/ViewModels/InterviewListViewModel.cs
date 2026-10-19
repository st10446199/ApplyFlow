using ApplyFlow.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ApplyFlow.Web.ViewModels
{
    public class InterviewListViewModel
    {
        public List<SelectListItem> Applications { get; set; } = new();

        public List<JobApplication> UpcomingInterviews { get; set; } = new();

        public List<JobApplication> PastInterviews { get; set; } = new();
    }
}