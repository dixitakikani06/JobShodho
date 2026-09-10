using JobShodho.Models;
using JobShodho.Services.Ai;
using JobShodho.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobShodho.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateInterviewPdf(JobApplication job, InterviewGenerationResponse content)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Interview Preparation").FontSize(20).Bold();
                    col.Item().PaddingTop(5).Text($"Company: {job.CompanyName}");
                    col.Item().Text($"Position: {job.JobTitle}");
                    col.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    var index = 1;
                    foreach (var qa in content.Questions)
                    {
                        col.Item().PaddingBottom(12).Column(qaCol =>
                        {
                            qaCol.Item().Text($"Question {index}").Bold().FontSize(12);
                            qaCol.Item().PaddingTop(2).Text(qa.Question);
                            qaCol.Item().PaddingTop(6).Text("Answer").Bold().FontSize(11);
                            qaCol.Item().PaddingTop(2).Text(qa.Answer);
                        });
                        index++;
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
