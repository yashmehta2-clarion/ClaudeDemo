using System.Net;
using System.Text.RegularExpressions;

namespace UserManagement.Tests;

// Address feature: end-to-end tests, sharing the helpers in AppFlowTests.cs.
public partial class AppFlowTests
{
    private async Task<(HttpClient Client, string Id)> AdminWithNewUserAsync(string email)
    {
        var c = NewClient();
        await LoginAsync(c, "admin@example.com", "Admin@12345");
        await PostFormAsync(c, "/Users/Create", "/Users/Create", new()
        {
            ["FirstName"] = "Addr", ["LastName"] = "Tester", ["Email"] = email, ["Role"] = "User",
            ["Password"] = "Passw0rd!", ["ConfirmPassword"] = "Passw0rd!"
        });
        var list = await c.GetStringAsync($"/Users?search={email}");
        return (c, Regex.Match(list, "/Users/Edit/(\\d+)").Groups[1].Value);
    }

    private static Dictionary<string, string> EditFields(string email, Dictionary<string, string>? address = null)
    {
        var f = new Dictionary<string, string>
        {
            ["FirstName"] = "Addr", ["LastName"] = "Tester", ["Email"] = email, ["Role"] = "User",
            ["Password"] = "", ["ConfirmPassword"] = ""
        };
        foreach (var kv in address ?? new()) f[kv.Key] = kv.Value;
        return f;
    }

    private static Dictionary<string, string> PuneAddress() => new()
    {
        ["Address1"] = "12 MG Road", ["Address2"] = "Near Station", ["City"] = "Pune",
        ["State"] = "Maharashtra", ["Country"] = "India", ["Pincode"] = "411001"
    };

    [Fact]
    public async Task Edit_page_shows_address_section_but_create_page_does_not()
    {
        var (c, id) = await AdminWithNewUserAsync("addr.page@example.com");

        var edit = await c.GetStringAsync($"/Users/Edit/{id}");
        Assert.Contains("id=\"addressSection\"", edit);
        Assert.Contains("<option>Maharashtra</option>", edit);
        Assert.Contains("<option>Russia</option>", edit);
        Assert.Contains("<option>India</option>", edit);

        var create = await c.GetStringAsync("/Users/Create");
        Assert.DoesNotContain("id=\"addressSection\"", create);
    }

    [Fact]
    public async Task Admin_can_add_update_and_clear_an_address()
    {
        const string email = "addr.crud@example.com";
        var (c, id) = await AdminWithNewUserAsync(email);

        // Add
        var add = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", EditFields(email, PuneAddress()));
        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);
        var page = await c.GetStringAsync($"/Users/Edit/{id}");
        Assert.Contains("value=\"12 MG Road\"", page);
        Assert.Contains("value=\"Near Station\"", page);
        Assert.Contains("value=\"Pune\"", page);
        Assert.Contains("value=\"411001\"", page);
        Assert.Contains("<option selected=\"selected\">Maharashtra</option>", page);
        Assert.Contains("<option selected=\"selected\">India</option>", page);

        // Update (switch country to Russia, change city)
        var changed = PuneAddress(); changed["City"] = "Moscow"; changed["Country"] = "Russia"; changed["Pincode"] = "101000";
        var upd = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", EditFields(email, changed));
        Assert.Equal(HttpStatusCode.Redirect, upd.StatusCode);
        page = await c.GetStringAsync($"/Users/Edit/{id}");
        Assert.Contains("value=\"Moscow\"", page);
        Assert.Contains("<option selected=\"selected\">Russia</option>", page);
        Assert.DoesNotContain("value=\"Pune\"", page);

        // Clear: blank address fields remove the address
        var clear = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", EditFields(email));
        Assert.Equal(HttpStatusCode.Redirect, clear.StatusCode);
        page = await c.GetStringAsync($"/Users/Edit/{id}");
        Assert.DoesNotContain("value=\"Moscow\"", page);
        Assert.DoesNotContain("selected=\"selected\">Russia", page);
    }

    [Fact]
    public async Task Editing_a_user_without_address_fields_does_not_require_an_address()
    {
        const string email = "addr.optional@example.com";
        var (c, id) = await AdminWithNewUserAsync(email);
        var res = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", EditFields(email));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
    }

    [Fact]
    public async Task Partial_or_invalid_address_is_rejected_and_nothing_is_saved()
    {
        const string email = "addr.invalid@example.com";
        var (c, id) = await AdminWithNewUserAsync(email);

        var partial = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", EditFields(email, new() { ["City"] = "Pune" }));
        var html = await partial.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, partial.StatusCode);
        Assert.Contains("Address 1 is required when an address is entered.", html);
        Assert.Contains("Please select a state.", html);
        Assert.Contains("Please select a country.", html);

        var badPin = PuneAddress(); badPin["Pincode"] = "0123";
        var res = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", EditFields(email, badPin));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("Pincode must be 6 digits and cannot start with 0.", await res.Content.ReadAsStringAsync());

        // A state that is not in the dropdown list (e.g. a tampered request) is rejected server-side.
        var badState = PuneAddress(); badState["State"] = "Atlantis";
        res = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", EditFields(email, badState));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("Please select a valid state.", await res.Content.ReadAsStringAsync());

        Assert.DoesNotContain("value=\"Pune\"", await c.GetStringAsync($"/Users/Edit/{id}"));
    }

    [Fact]
    public async Task Normal_user_cannot_open_or_save_an_address()
    {
        const string email = "addr.target@example.com";
        var (admin, id) = await AdminWithNewUserAsync(email);

        var user = NewClient();
        await RegisterAsync(user, "Plain", "Person", "addr.plain@example.com", "User");
        await LoginAsync(user, "addr.plain@example.com", "Passw0rd!");

        var get = await user.GetAsync($"/Users/Edit/{id}");
        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.Contains("/Account/AccessDenied", get.Headers.Location!.ToString());

        // Page the normal user is allowed to see must not leak address data.
        Assert.DoesNotContain("411001", await user.GetStringAsync("/Users?pageSize=50"));
    }

    [Fact]
    public async Task Deleting_a_user_removes_the_address_without_error()
    {
        const string email = "addr.delete@example.com";
        var (c, id) = await AdminWithNewUserAsync(email);
        await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", EditFields(email, PuneAddress()));

        var del = await PostFormAsync(c, "/Users", "/Users/Delete", new() { ["id"] = id });
        Assert.Equal(HttpStatusCode.Redirect, del.StatusCode);
        Assert.DoesNotContain(email, await c.GetStringAsync("/Users?search=addr.delete"));
    }

    [Fact]
    public async Task Password_is_required_client_side_on_create_but_not_on_edit()
    {
        var (c, id) = await AdminWithNewUserAsync("addr.pwd@example.com");

        var create = await c.GetStringAsync("/Users/Create");
        Assert.Contains("data-val-required=\"Password is required.\"", create);

        // Regression: Edit must not emit an empty data-val-required on Password (browser would demand a password).
        var edit = await c.GetStringAsync($"/Users/Edit/{id}");
        Assert.DoesNotContain("data-val-required=\"\"", edit);
    }
}
