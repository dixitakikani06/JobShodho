using System.Text;
using JobShodho.Models;

namespace JobShodho.Services.Ai;

/// <summary>Centralizes every AI prompt so wording lives in one place instead of scattered across controllers/services.</summary>
public static class PromptBuilder
{
    public static (string System, string User) BuildEmailPrompt(JobApplication job, CandidateProfile profile, string technologyBackground, Resume? selectedResume)
    {
        const string system = """
            You are helping a software developer apply for a job. Write a concise, professional job
            application email. Do not sound like AI, do not use exaggerated claims, and never invent
            experience, projects, certifications, or education that are not provided to you. Mention
            relevant skills only, keep it concise, and mention that the resume is attached. Return ONLY a
            JSON object of the exact shape {"subject": "...", "body": "..."} with no extra commentary.
            """;

        var sb = new StringBuilder();
        sb.AppendLine($"Candidate name: {profile.FullName}");
        sb.AppendLine($"Candidate experience: {profile.YearsOfExperience} years of professional software development experience.");
        sb.AppendLine($"Technology background: {technologyBackground}");
        if (!string.IsNullOrWhiteSpace(profile.Skills))
        {
            sb.AppendLine($"Skills: {profile.Skills}");
        }
        if (!string.IsNullOrWhiteSpace(profile.ProfessionalSummary))
        {
            sb.AppendLine($"Professional summary: {profile.ProfessionalSummary}");
        }
        if (selectedResume is not null)
        {
            sb.AppendLine($"Selected resume: {selectedResume.Name}");
        }
        sb.AppendLine();
        sb.AppendLine($"Company: {job.CompanyName}");
        sb.AppendLine($"Job title: {job.JobTitle}");
        sb.AppendLine("Job description:");
        sb.AppendLine(job.JobDescription);
        sb.AppendLine();
        sb.AppendLine("Requirements:");
        sb.AppendLine("- Personalize the email according to the job description.");
        sb.AppendLine("- Mention relevant skills only, do not invent experience.");
        sb.AppendLine("- Keep it concise and professional, do not sound robotic.");
        sb.AppendLine("- Do not mention that AI was used.");
        sb.AppendLine("- Include a professional subject line.");
        sb.AppendLine("- Mention that the resume is attached.");

        return (system, sb.ToString());
    }

    public static (string System, string User) BuildResumeSelectionPrompt(JobApplication job, List<Resume> resumes)
    {
        const string system = """
            You are matching a candidate's resumes to a job description. Compare the job description
            against each resume's extracted text and pick the single best match. Return ONLY a JSON object
            of the exact shape {"resumeId": number, "score": number (0-100), "reason": "..."} with no
            extra commentary. The resumeId MUST be one of the ids provided.
            """;

        var sb = new StringBuilder();
        sb.AppendLine($"Job title: {job.JobTitle}");
        sb.AppendLine($"Company: {job.CompanyName}");
        sb.AppendLine("Job description:");
        sb.AppendLine(job.JobDescription);
        sb.AppendLine();
        sb.AppendLine("Candidate resumes:");
        foreach (var resume in resumes)
        {
            sb.AppendLine($"--- Resume id={resume.Id}, name=\"{resume.Name}\" ---");
            sb.AppendLine(Truncate(resume.ExtractedText, 4000));
            sb.AppendLine();
        }

        return (system, sb.ToString());
    }

    public static (string System, string User) BuildInterviewPrompt(JobApplication job, CandidateProfile profile, string technologyBackground, Resume? selectedResume)
    {
        const string system = """
            You are preparing interview questions and answers for a software developer with approximately
            4 years of experience. Generate 10 to 15 questions covering technical, job-specific, practical
            scenario, .NET/C#, SQL, API/backend and behavioral topics as relevant to the job description.
            Write answers as a developer with about 4 years of experience would realistically answer them -
            not unrealistically senior, and never inventing experience the candidate does not have. Return
            ONLY a JSON object of the exact shape {"jobTitle": "...", "company": "...",
            "questions": [{"question": "...", "answer": "..."}]} with no extra commentary.
            """;

        var sb = new StringBuilder();
        sb.AppendLine($"Job title: {job.JobTitle}");
        sb.AppendLine($"Company: {job.CompanyName}");
        sb.AppendLine("Job description:");
        sb.AppendLine(job.JobDescription);
        sb.AppendLine();
        sb.AppendLine($"Candidate experience: {profile.YearsOfExperience} years.");
        sb.AppendLine($"Technology background: {technologyBackground}");
        if (selectedResume is not null && !string.IsNullOrWhiteSpace(selectedResume.ExtractedText))
        {
            sb.AppendLine("Candidate resume excerpt:");
            sb.AppendLine(Truncate(selectedResume.ExtractedText, 3000));
        }

        return (system, sb.ToString());
    }

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "(no text extracted)";
        }
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}
