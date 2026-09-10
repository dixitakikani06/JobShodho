namespace JobShodho.ViewModels;

public class ImportResultViewModel
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedEmptyCount { get; set; }
    public int DuplicateCount { get; set; }
    public int InvalidCount { get; set; }
    public List<string> Errors { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;
}
