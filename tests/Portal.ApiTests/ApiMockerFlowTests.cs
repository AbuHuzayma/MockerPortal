using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class ApiMockerFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    private static object BuildTestApiPayload(string code, bool isActive) => new
    {
        code,
        name = "Test API",
        description = "Seeded by ApiMockerFlowTests",
        environment = "DEV",
        isActive,
        endpoints = new[]
        {
            new
            {
                path = "verify/thing",
                httpMethod = "POST",
                isActive = true,
                responses = new object[]
                {
                    new
                    {
                        name = "Matched",
                        httpStatusCode = 200,
                        responseHeaders = "{\"Content-Type\":\"application/json\"}",
                        responseBody = "{\"echo\":\"{{request.header.X-Test-Case}}\",\"id\":\"{{uuid}}\"}",
                        delayMilliseconds = 0,
                        isActive = true,
                        priority = 1,
                        matchRules = new[]
                        {
                            new { source = "Header", field = "X-Test-Case", @operator = "Equals", expectedValue = "HELLO" }
                        }
                    },
                    new
                    {
                        name = "Default",
                        httpStatusCode = 404,
                        responseHeaders = (string?)null,
                        responseBody = "{\"code\":\"NOT_MATCHED\"}",
                        delayMilliseconds = 0,
                        isActive = true,
                        priority = 99,
                        matchRules = Array.Empty<object>()
                    }
                }
            }
        }
    };

    [Fact]
    public async Task List_without_authentication_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/mock-admin/apis");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReadOnly_user_cannot_manage_mocks()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/mock-admin/apis/RO-TEST", BuildTestApiPayload("RO-TEST", false));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_upsert_enable_and_the_serving_route_resolves_a_matched_response()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var upsertResponse = await client.PutAsJsonAsync("/api/v1/mock-admin/apis/FLOWTEST", BuildTestApiPayload("FLOWTEST", false));
        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);

        var enableResponse = await client.PostAsync("/api/v1/mock-admin/apis/FLOWTEST/enable", null);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Post, "/mock/FLOWTEST/verify/thing")
        {
            Content = JsonContent.Create(new { anything = "x" })
        };
        request.Headers.Add("X-Test-Case", "HELLO");

        var anonymousClient = factory.CreateClient();
        var mockResponse = await anonymousClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, mockResponse.StatusCode);
        var body = await mockResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.Equal("HELLO", body!["echo"]?.ToString());
        Assert.False(string.IsNullOrWhiteSpace(body["id"]?.ToString()));
    }

    [Fact]
    public async Task Serving_route_falls_back_to_ruleless_default_when_no_rule_matches()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");
        await client.PutAsJsonAsync("/api/v1/mock-admin/apis/FLOWTEST2", BuildTestApiPayload("FLOWTEST2", false));
        await client.PostAsync("/api/v1/mock-admin/apis/FLOWTEST2/enable", null);

        var request = new HttpRequestMessage(HttpMethod.Post, "/mock/FLOWTEST2/verify/thing")
        {
            Content = JsonContent.Create(new { anything = "x" })
        };
        request.Headers.Add("X-Test-Case", "SOMETHING_ELSE");

        var mockResponse = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, mockResponse.StatusCode);
        var body = await mockResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.Equal("NOT_MATCHED", body!["code"]?.ToString());
    }

    [Fact]
    public async Task Serving_route_returns_no_mock_configured_for_an_unknown_api_code()
    {
        var response = await factory.CreateClient().GetAsync("/mock/DOES-NOT-EXIST/anything");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.Equal("NO_MOCK_CONFIGURED", body!["code"]?.ToString());
    }

    [Fact]
    public async Task QA_user_can_enable_a_mock_without_a_preprod_scoped_permission()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("admin@portal.local");
        await admin.PutAsJsonAsync("/api/v1/mock-admin/apis/QAENABLE", BuildTestApiPayload("QAENABLE", false));

        var qa = await factory.CreateAuthenticatedClientAsync("qa@portal.local");
        var enableResponse = await qa.PostAsync("/api/v1/mock-admin/apis/QAENABLE/enable", null);

        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
    }

    [Fact]
    public async Task Disabled_mock_is_not_served()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("admin@portal.local");
        await admin.PutAsJsonAsync("/api/v1/mock-admin/apis/DISABLEDTEST", BuildTestApiPayload("DISABLEDTEST", false));
        // Never enabled.

        var request = new HttpRequestMessage(HttpMethod.Post, "/mock/DISABLEDTEST/verify/thing")
        {
            Content = JsonContent.Create(new { anything = "x" })
        };
        request.Headers.Add("X-Test-Case", "HELLO");

        var mockResponse = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, mockResponse.StatusCode);
        var body = await mockResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.Equal("NO_MOCK_CONFIGURED", body!["code"]?.ToString());
    }
}
