using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Portal.Api.Authorization;
using Portal.Api.Common;
using Portal.Application.Admin;
using Portal.Application.Audit;
using Portal.Application.Common;

namespace Portal.Api.Controllers;

/// <summary>
/// User/role/permission administration — docs/15 Phase 7. Roles and the
/// permission catalog are read-only here (PermissionCatalog is a code change,
/// reviewed — docs/07 §6); only user accounts and their role assignment are
/// admin-editable.
/// </summary>
[Route("api/v1/admin")]
[HasPermission(PermissionCodes.AdminUsers)]
public sealed class AdminUsersController(
    IAdminUserService adminUserService,
    IAuditService auditService,
    IEnvironmentContext environmentContext) : ApiControllerBase
{
    private Guid CallerId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    private string CallerEmail => User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "unknown";

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await adminUserService.ListUsersAsync(ct), CorrelationId));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await adminUserService.CreateUserAsync(request, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await WriteAudit("CREATE_USER", result.Value!.Email, ct);
        return Ok(ApiResponse<object>.Ok(result.Value, CorrelationId));
    }

    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, [FromBody] SetRoleRequest request, CancellationToken ct)
    {
        var result = await adminUserService.SetRoleAsync(id, request.Role, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await WriteAudit("SET_ROLE", $"{result.Value!.Email} -> {request.Role}", ct);
        return Ok(ApiResponse<object>.Ok(result.Value, CorrelationId));
    }

    [HttpPost("users/{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct) => await SetActive(id, true, ct);

    [HttpPost("users/{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct) => await SetActive(id, false, ct);

    private async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken ct)
    {
        var result = await adminUserService.SetActiveAsync(id, isActive, CallerId, ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == AdminErrorCodes.CannotDisableSelf ? StatusCodes.Status400BadRequest : StatusCodes.Status404NotFound;
            return StatusCode(status, ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await WriteAudit(isActive ? "ENABLE_USER" : "DISABLE_USER", result.Value!.Email, ct);
        return Ok(ApiResponse<object>.Ok(result.Value, CorrelationId));
    }

    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await adminUserService.ResetPasswordAsync(id, request.NewPassword, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await WriteAudit("RESET_PASSWORD", id.ToString(), ct);
        return Ok(ApiResponse<object>.Ok(new { reset = true }, CorrelationId));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await adminUserService.ListRolesAsync(ct), CorrelationId));

    [HttpGet("permissions")]
    public IActionResult ListPermissions() =>
        Ok(ApiResponse<object>.Ok(adminUserService.ListPermissionCatalog(), CorrelationId));

    private async Task WriteAudit(string operation, string detail, CancellationToken ct) =>
        await auditService.WriteAsync(new AuditEntry
        {
            Username = CallerEmail,
            Environment = environmentContext.Name,
            Screen = "ADMIN",
            Operation = operation,
            Entity = "User",
            NewValue = detail,
            Result = AuditResults.Success,
            CorrelationId = CorrelationId,
        }, ct);
}

public sealed record SetRoleRequest(string Role);
public sealed record ResetPasswordRequest(string NewPassword);
