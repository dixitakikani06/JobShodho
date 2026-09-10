using JobShodho.Services.Validation;

namespace JobShodho.Tests;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("hr@abc.com")]
    [InlineData("jobs.team@xyz-co.io")]
    [InlineData("first.last+tag@sub.domain.co")]
    public void IsValid_AcceptsWellFormedEmails(string email)
    {
        Assert.True(EmailValidator.IsValid(email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("no-domain@")]
    [InlineData("@no-local-part.com")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void IsValid_RejectsMalformedEmails(string? email)
    {
        Assert.False(EmailValidator.IsValid(email));
    }
}
