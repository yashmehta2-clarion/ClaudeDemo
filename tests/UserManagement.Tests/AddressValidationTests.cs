using System.ComponentModel.DataAnnotations;
using UserManagement.Web.Models;
using UserManagement.Web.ViewModels;

namespace UserManagement.Tests;

/// <summary>Unit tests for the optional address on the admin Edit User form.</summary>
public class AddressValidationTests
{
    private static UserFormViewModel EditModel() => new()
    {
        Id = 1, FirstName = "Asha", LastName = "Rao", Email = "asha@example.com", Role = UserRole.User
    };

    private static UserFormViewModel FullAddress()
    {
        var m = EditModel();
        m.Address1 = "12 MG Road"; m.City = "Pune"; m.State = "Maharashtra"; m.Country = "India"; m.Pincode = "411001";
        return m;
    }

    private static List<ValidationResult> Validate(UserFormViewModel m)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(m, new ValidationContext(m), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> r, string member) => r.Any(x => x.MemberNames.Contains(member));

    [Fact]
    public void Address_options_contain_expected_countries_and_indian_states()
    {
        Assert.Equal(new[] { "India", "Russia" }, AddressOptions.Countries);
        Assert.Equal(36, AddressOptions.IndianStates.Count);          // 28 states + 8 union territories
        Assert.Equal(AddressOptions.IndianStates.Count, AddressOptions.IndianStates.Distinct().Count());
        Assert.Contains("Maharashtra", AddressOptions.IndianStates);
        Assert.Contains("Delhi", AddressOptions.IndianStates);
    }

    [Fact]
    public void Blank_address_is_valid_and_treated_as_no_address()
    {
        var m = EditModel();
        Assert.False(m.HasAddress);
        Assert.Empty(Validate(m));
    }

    [Fact]
    public void Complete_address_is_valid()
    {
        Assert.Empty(Validate(FullAddress()));
    }

    [Fact]
    public void Partial_address_requires_the_mandatory_fields()
    {
        var m = EditModel();
        m.City = "Pune";                       // only one field filled
        var r = Validate(m);
        Assert.True(HasErrorFor(r, nameof(m.Address1)));
        Assert.True(HasErrorFor(r, nameof(m.State)));
        Assert.True(HasErrorFor(r, nameof(m.Country)));
        Assert.True(HasErrorFor(r, nameof(m.Pincode)));
        Assert.False(HasErrorFor(r, nameof(m.City)));
        Assert.False(HasErrorFor(r, nameof(m.Address2)));   // Address 2 stays optional
    }

    [Fact]
    public void Address2_is_optional()
    {
        var m = FullAddress();
        m.Address2 = null;
        Assert.Empty(Validate(m));
    }

    [Theory]
    [InlineData("12345")]      // too short
    [InlineData("1234567")]    // too long
    [InlineData("012345")]     // starts with 0
    [InlineData("41100A")]     // not numeric
    public void Invalid_pincode_is_rejected(string pincode)
    {
        var m = FullAddress();
        m.Pincode = pincode;
        Assert.True(HasErrorFor(Validate(m), nameof(m.Pincode)));
    }

    [Theory]
    [InlineData("411001")]
    [InlineData("110001")]
    [InlineData("101000")]     // Moscow-style postal code
    public void Valid_pincode_is_accepted(string pincode)
    {
        var m = FullAddress();
        m.Pincode = pincode;
        Assert.False(HasErrorFor(Validate(m), nameof(m.Pincode)));
    }

    [Fact]
    public void State_and_country_must_come_from_the_dropdown_lists()
    {
        var m = FullAddress();
        m.State = "Atlantis"; m.Country = "France";
        var r = Validate(m);
        Assert.True(HasErrorFor(r, nameof(m.State)));
        Assert.True(HasErrorFor(r, nameof(m.Country)));

        m.Country = "Russia";                  // the second allowed country
        Assert.False(HasErrorFor(Validate(m), nameof(m.Country)));
    }
}
