using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UserManagement.Tests;

public partial class AppFlowTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;
    public AppFlowTests(TestAppFactory factory) => _factory = factory;

    private HttpClient NewClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost")
    });

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    private static async Task<string> GetTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        return TokenRegex().Match(html).Groups[1].Value;
    }

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string formUrl, string postUrl, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await GetTokenAsync(client, formUrl);
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(fields));
    }

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient c, string first, string last, string email, string role, string password = "Passw0rd!") =>
        PostFormAsync(c, "/Account/Register", "/Account/Register", new()
        {
            ["FirstName"] = first, ["LastName"] = last, ["Email"] = email, ["Role"] = role,
            ["Password"] = password, ["ConfirmPassword"] = password
        });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient c, string email, string password) =>
        PostFormAsync(c, "/Account/Login", "/Account/Login", new() { ["Username"] = email, ["Password"] = password });

    [Fact]
    public async Task Anonymous_user_is_redirected_to_login()
    {
        var res = await NewClient().GetAsync("/Users");
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Account/Login", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Register_rejects_invalid_input()
    {
        var c = NewClient();
        var res = await PostFormAsync(c, "/Account/Register", "/Account/Register", new()
        {
            ["FirstName"] = "", ["LastName"] = "X1", ["Email"] = "not-an-email", ["Role"] = "",
            ["Password"] = "short", ["ConfirmPassword"] = "different"
        });
        var html = await res.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("First name is required.", html);
        Assert.Contains("Enter a valid email address.", html);
        Assert.Contains("Please select a role.", html);
        Assert.Contains("Passwords do not match.", html);
    }

    [Fact]
    public async Task Register_then_login_redirects_to_user_list()
    {
        var c = NewClient();
        var reg = await RegisterAsync(c, "Jane", "Doe", "jane.flow@example.com", "User");
        Assert.Equal(HttpStatusCode.Redirect, reg.StatusCode);

        var dup = await RegisterAsync(c, "Jane", "Again", "JANE.flow@example.com", "User");
        Assert.Contains("An account with this email already exists.", await dup.Content.ReadAsStringAsync());

        var bad = await LoginAsync(c, "jane.flow@example.com", "WrongPass1");
        Assert.Contains("Invalid username or password.", await bad.Content.ReadAsStringAsync());

        var ok = await LoginAsync(c, "jane.flow@example.com", "Passw0rd!");
        Assert.Equal(HttpStatusCode.Redirect, ok.StatusCode);
        Assert.Equal("/Users", ok.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Normal_user_sees_only_normal_users_and_cannot_manage()
    {
        var c = NewClient();
        await RegisterAsync(c, "Hidden", "Adminperson", "hidden.admin@example.com", "Admin");
        await RegisterAsync(c, "Visible", "Userperson", "visible.user@example.com", "User");
        await LoginAsync(c, "visible.user@example.com", "Passw0rd!");

        var html = await c.GetStringAsync("/Users?pageSize=50");
        Assert.Contains("visible.user@example.com", html);
        Assert.DoesNotContain("hidden.admin@example.com", html);
        Assert.DoesNotContain("mailto:admin@example.com", html);
        Assert.DoesNotContain("Create user", html);

        var create = await c.GetAsync("/Users/Create");
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        Assert.Contains("/Account/AccessDenied", create.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Admin_can_create_filter_sort_edit_and_delete()
    {
        var c = NewClient();
        var login = await LoginAsync(c, "admin@example.com", "Admin@12345");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var created = await PostFormAsync(c, "/Users/Create", "/Users/Create", new()
        {
            ["FirstName"] = "Zed", ["LastName"] = "Crudtest", ["Email"] = "zed.crud@example.com", ["Role"] = "User",
            ["Password"] = "Passw0rd!", ["ConfirmPassword"] = "Passw0rd!"
        });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);

        var list = await c.GetStringAsync("/Users?search=crudtest");
        Assert.Contains("zed.crud@example.com", list);
        Assert.DoesNotContain("mailto:admin@example.com", list);

        var admins = await c.GetStringAsync("/Users?role=Admin&pageSize=50");
        Assert.Contains("admin@example.com", admins);
        Assert.DoesNotContain("zed.crud@example.com", admins);

        var sorted = await c.GetStringAsync("/Users?sortBy=email&sortDir=desc&pageSize=50");
        Assert.True(sorted.IndexOf("zed.crud@example.com", StringComparison.Ordinal) < sorted.IndexOf("mailto:admin@example.com", StringComparison.Ordinal));

        var id = Regex.Match(list, "/Users/Edit/(\\d+)").Groups[1].Value;
        var edited = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", new()
        {
            ["FirstName"] = "Zed", ["LastName"] = "Renamed", ["Email"] = "zed.crud@example.com", ["Role"] = "Admin",
            ["Password"] = "", ["ConfirmPassword"] = ""
        });
        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        Assert.Contains("Renamed", await c.GetStringAsync("/Users?search=zed.crud"));

        var deleted = await PostFormAsync(c, "/Users", "/Users/Delete", new() { ["id"] = id });
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.DoesNotContain("zed.crud@example.com", await c.GetStringAsync("/Users?search=zed"));
    }

    [Fact]
    public async Task Admin_cannot_delete_or_demote_self()
    {
        var c = NewClient();
        await LoginAsync(c, "admin@example.com", "Admin@12345");
        var list = await c.GetStringAsync("/Users?role=Admin&search=admin@example.com");
        var id = Regex.Match(list, "/Users/Edit/(\\d+)").Groups[1].Value;

        var demote = await PostFormAsync(c, $"/Users/Edit/{id}", $"/Users/Edit/{id}", new()
        {
            ["FirstName"] = "System", ["LastName"] = "Administrator", ["Email"] = "admin@example.com", ["Role"] = "User"
        });
        Assert.Contains("You cannot remove the Admin role from your own account.", await demote.Content.ReadAsStringAsync());

        await PostFormAsync(c, "/Users", "/Users/Delete", new() { ["id"] = id });
        Assert.Contains("admin@example.com", await c.GetStringAsync("/Users?role=Admin"));
    }
}
