using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Portal.Infrastructure.Identity;
using Xunit;

namespace Portal.ApiTests;

/// <summary>Covers docs/14-testing-strategy.md §4's minimum bar for Phase 1: the full auth flow.</summary>
public class AuthFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    private const string AdminEmail = "admin@portal.local";

    [Fact]
    public async Task Login_with_valid_dev_seed_credentials_returns_access_token_and_permissions()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AdminEmail, password = IdentitySeeder.DevSeedPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<LoginData>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.False(string.IsNullOrWhiteSpace(body.Data!.AccessToken));
        Assert.Contains("admin.users", body.Data.User.Permissions);
        Assert.Contains("Administrator", body.Data.User.Roles);

        // Refresh token must never appear in the JSON body — it's httpOnly-cookie only.
        Assert.DoesNotContain("refreshToken", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith("refreshToken=", StringComparison.Ordinal)
            && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_invalid_credentials()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AdminEmail, password = "definitely-wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.False(body!.Success);
        Assert.Equal("INVALID_CREDENTIALS", body.Error!.Code);
    }

    [Fact]
    public async Task Login_with_missing_fields_returns_validation_error()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("VALIDATION_FAILED", body!.Error!.Code);
    }

    [Fact]
    public async Task Me_without_a_token_is_unauthenticated()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_a_valid_access_token_returns_the_current_user()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AdminEmail, password = IdentitySeeder.DevSeedPassword });
        var loginBody = await login.Content.ReadFromJsonAsync<DataEnvelope<LoginData>>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Data!.AccessToken);
        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<MeData>>();
        Assert.Equal(AdminEmail, body!.Data!.Email);
    }

    [Fact]
    public async Task Refresh_rotates_the_cookie_and_logout_revokes_it()
    {
        // WebApplicationFactoryClientOptions.HandleCookies defaults to true, so this
        // client automatically carries the httpOnly refresh-token cookie set by login.
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AdminEmail, password = IdentitySeeder.DevSeedPassword });
        var loginBody = await login.Content.ReadFromJsonAsync<DataEnvelope<LoginData>>();

        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<DataEnvelope<LoginData>>();
        Assert.NotEqual(loginBody!.Data!.AccessToken, refreshBody!.Data!.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshBody.Data.AccessToken);
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var refreshAfterLogout = await client.PostAsync("/api/v1/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    private sealed record LoginData(string AccessToken, DateTime AccessTokenExpiresAtUtc, MeData User);
    private sealed record MeData(Guid Id, string Email, string FullName, string[] Roles, string[] Permissions, string Environment);
}
