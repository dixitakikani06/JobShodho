using JobShodho.Models;
using JobShodho.Models.Enums;

namespace JobShodho.ViewModels;

public class JobListFilter
{
    public EmailStatus? Status { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsEmailSent { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class JobListViewModel
{
    public JobListFilter Filter { get; set; } = new();
    public PagedResult<JobApplication> Results { get; set; } = new();
}
