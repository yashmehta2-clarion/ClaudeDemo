namespace UserManagement.Web.ViewModels;

internal static class ValidationPatterns
{
    /// <summary>
    /// Letters (including Latin accented letters), spaces, apostrophes, dots and hyphens.
    /// Kept free of \p{..} classes so the same pattern works for client-side (JavaScript) validation.
    /// </summary>
    public const string PersonName = @"^[A-Za-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u00FF][A-Za-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u00FF '.\-]*$";
    public const string PersonNameMessage = "{0} may only contain letters, spaces, apostrophes, dots and hyphens.";

    /// <summary>
    /// Email address that must have a domain with a dot and a top-level domain of 2+ letters
    /// (e.g. abc@aaa.com; rejects abc@aaa). Also rejects consecutive dots, spaces, empty domain labels
    /// and labels starting/ending with a hyphen. Uses syntax supported by both .NET and JavaScript
    /// so the same rule runs server-side and in the browser (jQuery unobtrusive validation).
    /// </summary>
    public const string Email =
        @"^(?!.*\.\.)[A-Za-z0-9._%+\-]+@[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?)*\.[A-Za-z]{2,}$";
    public const string EmailMessage = "Enter a valid email address.";

    /// <summary>At least one upper-case letter, one lower-case letter and one digit.</summary>
    public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$";
    public const string PasswordMessage = "Password must contain at least one upper-case letter, one lower-case letter and one digit.";
}
