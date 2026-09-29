using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class AdminFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task ListUsers_without_authentication_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReadOnly_user_cannot_list_users()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_a_user_assign_a_role_disable_and_reset_password()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var createResponse = await admin.PostAsJsonAsync("/api/v1/admin/users", new
        {
            email = "new.tester@portal.local",
            fullName = "New Tester",
            password = "Some-Strong-Passw0rd!",
            role = "QA",
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<DataEnvelope<UserDto>>();
        Assert.Contains("QA", created!.Data!.Roles);
        Assert.True(created.Data.IsActive);

        var roleResponse = await admin.PutAsJsonAsync($"/api/v1/admin/users/{created.Data.Id}/role", new { role = "Developer" });
        Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);
        var afterRole = await roleResponse.Content.ReadFromJsonAsync<DataEnvelope<UserDto>>();
        Assert.Contains("Developer", afterRole!.Data!.Roles);

        var disableResponse = await admin.PostAsync($"/api/v1/admin/users/{created.Data.Id}/disable", null);
        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
        var afterDisable = await disableResponse.Content.ReadFromJsonAsync<DataEnvelope<UserDto>>();
        Assert.False(afterDisable!.Data!.IsActive);

        var disabledLogin = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new { email = "new.tester@portal.local", password = "Some-Strong-Passw0rd!" });
        Assert.Equal(HttpStatusCode.Unauthorized, disabledLogin.StatusCode);

        var resetResponse = await admin.PostAsJsonAsync($"/api/v1/admin/users/{created.Data.Id}/reset-password", new { newPassword = "Another-Strong-Passw0rd!" });
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_disable_their_own_account()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var me = await (await admin.GetAsync("/api/v1/auth/me")).Content.ReadFromJsonAsync<DataEnvelope<MeDto>>();
        var response = await admin.PostAsync($"/api/v1/admin/users/{me!.Data!.Id}/disable", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("CANNOT_DISABLE_SELF", body!.Error!.Code);
    }

    [Fact]
    public async Task Admin_can_list_roles_and_the_permission_catalog()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var roles = await (await admin.GetAsync("/api/v1/admin/roles")).Content.ReadFromJsonAsync<DataEnvelope<List<RoleDto>>>();
        Assert.Contains(roles!.Data!, r => r.Name == "Administrator" && r.PermissionCodes.Contains("admin.users"));

        var permissions = await (await admin.GetAsync("/api/v1/admin/permissions")).Content.ReadFromJsonAsync<DataEnvelope<List<PermissionDto>>>();
        Assert.Contains(permissions!.Data!, p => p.Code == "api-mocker.view");
    }

    [Fact]
    public async Task Admin_can_list_screens_read_only()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await admin.GetAsync("/api/v1/admin/screens");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<List<ScreenAdminDto>>>();
        Assert.NotEmpty(body!.Data!);
    }

    [Fact]
    public async Task Admin_can_view_integration_status()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await admin.GetAsync("/api/v1/admin/integrations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<IntegrationsStatusDto>>();
        Assert.NotEmpty(body!.Data!.Providers);
        Assert.Contains(body.Data.Providers, p => p.Name == "Customer" && p.Mode == "Mock");
    }

    [Fact]
    public async Task Audit_endpoint_returns_entries_written_by_other_actions()
    {
        var admin = await factory.CreateAuthenticatedClientAsync("admin@portal.local");
        await admin.PostAsJsonAsync("/api/v1/admin/users", new
        {
            email = "audit.trigger@portal.local",
            fullName = "Audit Trigger",
            password = "Some-Strong-Passw0rd!",
            role = "QA",
        });

        var response = await admin.GetAsync("/api/v1/audit?screen=ADMIN&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<AuditPageDto>>();
        Assert.True(body!.Data!.TotalCount > 0);
        Assert.Contains(body.Data.Items, i => i.Operation == "CREATE_USER");
    }

    [Fact]
    public async Task ReadOnly_user_can_view_audit_log_but_not_admin_users()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var auditResponse = await client.GetAsync("/api/v1/audit");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var usersResponse = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);
    }

    private sealed record UserDto(Guid Id, string Email, string FullName, List<string> Roles, bool IsActive);
    private sealed record MeDto(Guid Id, string Email);
    private sealed record RoleDto(string Name, string? Description, List<string> PermissionCodes);
    private sealed record PermissionDto(string Code, string Description, string Category);
    private sealed record ScreenAdminDto(string Code, string Name);
    private sealed record ProviderStatusDto(string Name, string Mode);
    private sealed record IntegrationsStatusDto(string Environment, List<ProviderStatusDto> Providers);
    private sealed record AuditEntryDto(Guid Id, string Username, string Operation);
    private sealed record AuditPageDto(List<AuditEntryDto> Items, int TotalCount, int Page, int PageSize);
}
