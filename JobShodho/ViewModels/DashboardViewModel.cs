using JobShodho.Models;

namespace JobShodho.ViewModels;

public class DashboardViewModel
{
    public int TotalJobs { get; set; }
    public int Pending { get; set; }
    public int ReadyForReview { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }

    public int SentToday { get; set; }
    public int FailedToday { get; set; }
    public int RemainingToSend { get; set; }

    public List<JobApplication> RecentApplications { get; set; } = new();
}
