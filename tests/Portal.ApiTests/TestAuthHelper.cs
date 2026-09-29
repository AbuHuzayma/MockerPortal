using System.Net.Http.Headers;
using System.Net.Http.Json;
using Portal.Infrastructure.Identity;

namespace Portal.ApiTests;

internal static class TestAuthHelper
{
    private sealed record LoginData(string AccessToken);
    private sealed record LoginEnvelope(bool Success, LoginData? Data);

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this PortalApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = IdentitySeeder.DevSeedPassword });
        var body = await login.Content.ReadFromJsonAsync<LoginEnvelope>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
        return client;
    }
}
