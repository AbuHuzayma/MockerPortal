using Microsoft.AspNetCore.Mvc;
using Portal.Api.Authorization;
using Portal.Api.Common;
using Portal.Application.Audit;
using Portal.Application.Common;
using Portal.Application.Mocking;

namespace Portal.Api.Controllers;

[Route("api/v1/mock-admin")]
[HasPermission(PermissionCodes.ApiMockerView)]
public sealed class MockAdminController(
    IMockAdminService mockAdminService,
    IAuditService auditService,
    IEnvironmentContext environmentContext) : ApiControllerBase
{
    [HttpGet("apis")]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mockAdminService.ListAsync(ct), CorrelationId));

    [HttpGet("apis/{code}")]
    public async Task<IActionResult> Get(string code, CancellationToken ct)
    {
        var result = await mockAdminService.GetAsync(code, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.Value, CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.ApiMockerManage)]
    [HttpPut("apis/{code}")]
    public async Task<IActionResult> Upsert(string code, [FromBody] MockApiDto body, CancellationToken ct)
    {
        var dto = body with { Code = code };
        var saved = await mockAdminService.UpsertAsync(dto, ct);

        await WriteAudit(code, "UPSERT", $"{saved.Endpoints.Count} endpoint(s)", ct);

        return Ok(ApiResponse<object>.Ok(saved, CorrelationId));
    }

    [HasPermission(PermissionCodes.ApiMockerManage)]
    [HttpDelete("apis/{code}")]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        var result = await mockAdminService.DeleteAsync(code, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await WriteAudit(code, "DELETE", null, ct);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }, CorrelationId));
    }

    [HasPermission(PermissionCodes.ApiMockerEnable, environmentScoped: true)]
    [HttpPost("apis/{code}/enable")]
    public async Task<IActionResult> Enable(string code, CancellationToken ct) => await SetActive(code, true, ct);

    [HasPermission(PermissionCodes.ApiMockerDisable)]
    [HttpPost("apis/{code}/disable")]
    public async Task<IActionResult> Disable(string code, CancellationToken ct) => await SetActive(code, false, ct);

    private async Task<IActionResult> SetActive(string code, bool isActive, CancellationToken ct)
    {
        var result = await mockAdminService.SetActiveAsync(code, isActive, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await WriteAudit(code, isActive ? "ENABLE" : "DISABLE", null, ct);

        return Ok(ApiResponse<object>.Ok(result.Value, CorrelationId));
    }

    private async Task WriteAudit(string mockApiCode, string operation, string? detail, CancellationToken ct)
    {
        await auditService.WriteAsync(new AuditEntry
        {
            Username = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "unknown",
            Environment = environmentContext.Name,
            Screen = "API_MOCKER",
            Operation = operation,
            Entity = "MockApi",
            Field = "code",
            NewValue = mockApiCode + (detail is null ? "" : $" ({detail})"),
            Result = AuditResults.Success,
            CorrelationId = CorrelationId,
        }, ct);
    }
}
