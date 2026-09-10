namespace JobShodho.ViewModels;

public class BatchSendResultViewModel
{
    public int Claimed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<string> Messages { get; set; } = new();
}
