using System.Text.Json;
using JobShodho.Services.Ai;

namespace JobShodho.Tests;

public class AiDtoParsingTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void EmailGenerationResponse_ParsesExpectedShape()
    {
        const string json = """{"subject": "Application for .NET Developer", "body": "Dear Hiring Team, ..."}""";

        var result = JsonSerializer.Deserialize<EmailGenerationResponse>(json, Options);

        Assert.NotNull(result);
        Assert.Equal("Application for .NET Developer", result!.Subject);
        Assert.Equal("Dear Hiring Team, ...", result.Body);
    }

    [Fact]
    public void ResumeSelectionResponse_ParsesExpectedShape()
    {
        const string json = """{"resumeId": 3, "score": 87, "reason": "Strong match for ASP.NET Core and SQL Server."}""";

        var result = JsonSerializer.Deserialize<ResumeSelectionResponse>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(3, result!.ResumeId);
        Assert.Equal(87, result.Score);
        Assert.Equal("Strong match for ASP.NET Core and SQL Server.", result.Reason);
    }

    [Fact]
    public void InterviewGenerationResponse_ParsesQuestionsList()
    {
        const string json = """
            {
              "jobTitle": ".NET Developer",
              "company": "ABC Ltd",
              "questions": [
                { "question": "How have you used dependency injection in ASP.NET Core?", "answer": "..." },
                { "question": "How do you optimize Entity Framework queries?", "answer": "..." }
              ]
            }
            """;

        var result = JsonSerializer.Deserialize<InterviewGenerationResponse>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(".NET Developer", result!.JobTitle);
        Assert.Equal(2, result.Questions.Count);
        Assert.Equal("How have you used dependency injection in ASP.NET Core?", result.Questions[0].Question);
    }
}
