using System.Text.RegularExpressions;

namespace JobShodho.Services.Validation;

public static partial class EmailValidator
{
    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return EmailRegex().IsMatch(email.Trim());
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
