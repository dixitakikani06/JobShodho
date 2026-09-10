namespace JobShodho.Models.Enums;

public enum EmailStatus
{
    Pending = 0,
    Generating = 1,
    ReadyForReview = 2,
    Sending = 3,
    Sent = 4,
    Failed = 5
}
