using JobShodho.Models;
using JobShodho.Services.Ai;

namespace JobShodho.ViewModels;

public class ReviewViewModel
{
    public JobApplication Job { get; set; } = null!;
    public List<Resume> AvailableResumes { get; set; } = new();
    public InterviewGenerationResponse? Interview { get; set; }
    public string? AiErrorMessage { get; set; }
}
