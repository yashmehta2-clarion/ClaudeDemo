namespace UserManagement.Web.ViewModels;

internal static class ValidationPatterns
{
    /// <summary>
    /// Letters (including Latin accented letters), spaces, apostrophes, dots and hyphens.
    /// Kept free of \p{..} classes so the same pattern works for client-side (JavaScript) validation.
    /// </summary>
    public const string PersonName = @"^[A-Za-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u00FF][A-Za-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u00FF '.\-]*$";
    public const string PersonNameMessage = "{0} may only contain letters, spaces, apostrophes, dots and hyphens.";

    /// <summary>At least one upper-case letter, one lower-case letter and one digit.</summary>
    public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$";
    public const string PasswordMessage = "Password must contain at least one upper-case letter, one lower-case letter and one digit.";
}
