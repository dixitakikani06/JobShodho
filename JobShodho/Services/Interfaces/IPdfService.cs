using JobShodho.Models;
using JobShodho.Services.Ai;

namespace JobShodho.Services.Interfaces;

public interface IPdfService
{
    byte[] GenerateInterviewPdf(JobApplication job, InterviewGenerationResponse content);
}
