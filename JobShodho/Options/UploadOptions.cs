namespace JobShodho.Options;

public class UploadOptions
{
    public const string SectionName = "Uploads";

    public long MaxResumeSizeBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedResumeExtensions { get; set; } = [".pdf", ".docx"];
}
