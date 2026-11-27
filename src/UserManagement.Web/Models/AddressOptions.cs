namespace UserManagement.Web.Models;

/// <summary>Fixed dropdown values for the address form. Used for rendering and for server-side validation.</summary>
public static class AddressOptions
{
    public static readonly IReadOnlyList<string> Countries = ["India", "Russia"];

    /// <summary>The 28 Indian states followed by the 8 union territories.</summary>
    public static readonly IReadOnlyList<string> IndianStates =
    [
        "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar", "Chhattisgarh", "Goa", "Gujarat", "Haryana",
        "Himachal Pradesh", "Jharkhand", "Karnataka", "Kerala", "Madhya Pradesh", "Maharashtra", "Manipur",
        "Meghalaya", "Mizoram", "Nagaland", "Odisha", "Punjab", "Rajasthan", "Sikkim", "Tamil Nadu", "Telangana",
        "Tripura", "Uttar Pradesh", "Uttarakhand", "West Bengal",
        "Andaman and Nicobar Islands", "Chandigarh", "Dadra and Nagar Haveli and Daman and Diu", "Delhi",
        "Jammu and Kashmir", "Ladakh", "Lakshadweep", "Puducherry"
    ];
}
