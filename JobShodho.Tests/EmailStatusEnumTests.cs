using JobShodho.Models.Enums;

namespace JobShodho.Tests;

/// <summary>
/// EmailBatchService issues raw SQL against the numeric EmailStatus values (see the UPDATE ... OUTPUT
/// statements in ClaimNextBatchAsync/ClaimSpecificAsync). If someone reorders this enum, those raw SQL
/// statements would silently start claiming/skipping the wrong rows. This test pins the values down.
/// </summary>
public class EmailStatusEnumTests
{
    [Fact]
    public void EnumValues_MatchValuesHardcodedInEmailBatchServiceRawSql()
    {
        Assert.Equal(0, (int)EmailStatus.Pending);
        Assert.Equal(1, (int)EmailStatus.Generating);
        Assert.Equal(2, (int)EmailStatus.ReadyForReview);
        Assert.Equal(3, (int)EmailStatus.Sending);
        Assert.Equal(4, (int)EmailStatus.Sent);
        Assert.Equal(5, (int)EmailStatus.Failed);
    }
}
