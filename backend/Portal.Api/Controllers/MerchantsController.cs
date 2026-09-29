using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Portal.Api.Authorization;
using Portal.Api.Common;
using Portal.Application.Audit;
using Portal.Application.Common;
using Portal.Application.Merchants;
using Portal.Domain.Merchants;

namespace Portal.Api.Controllers;

[HasPermission(PermissionCodes.MerchantView)]
public sealed class MerchantsController(
    IMerchantService merchantService,
    IB2BServiceClient b2bServiceClient,
    IAuditService auditService,
    IEnvironmentContext environmentContext) : ApiControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<object>.Fail(
                new ApiError { Code = "VALIDATION_FAILED", Message = "A search name is required." }, CorrelationId));
        }

        var result = await merchantService.SearchByNameAsync(name, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.Value, CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HttpGet("{merchantId}")]
    public async Task<IActionResult> GetProfile(string merchantId, CancellationToken ct)
    {
        var result = await merchantService.GetProfileAsync(merchantId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.Value, CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.MerchantUpdate)]
    [HttpPut("{merchantId}")]
    public async Task<IActionResult> Update(string merchantId, [FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        var normalized = body.ToDictionary(kvp => kvp.Key, kvp => NormalizeJsonValue(kvp.Value));

        var result = await merchantService.UpdateAsync(merchantId, normalized, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await auditService.WriteAsync(BuildAuditEntries(merchantId, result.Value!.Before, result.Value.After), ct);

        return Ok(ApiResponse<object>.Ok(result.Value.After, CorrelationId));
    }

    [HasPermission(PermissionCodes.MerchantB2BView)]
    [HttpGet("{merchantId}/b2b")]
    public async Task<IActionResult> GetB2BStatus(string merchantId, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await b2bServiceClient.GetStatusAsync(merchantId, ct), CorrelationId));

    [HasPermission(PermissionCodes.MerchantB2BUpdate)]
    [HttpPost("{merchantId}/b2b")]
    public async Task<IActionResult> AddToB2B(string merchantId, CancellationToken ct)
    {
        var before = await b2bServiceClient.GetStatusAsync(merchantId, ct);
        var after = await b2bServiceClient.AddMerchantToB2BAsync(merchantId, ct);

        await auditService.WriteAsync([new AuditEntry
        {
            Username = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "unknown",
            Environment = environmentContext.Name,
            MerchantId = merchantId,
            Screen = "MERCHANT_B2B",
            Operation = "ADD_TO_B2B",
            Entity = "B2BSubscription",
            Field = "status",
            OldValue = before.Status,
            NewValue = after.Status,
            Result = AuditResults.Success,
            CorrelationId = CorrelationId,
        }], ct);

        return Ok(ApiResponse<object>.Ok(after, CorrelationId));
    }

    private static object? NormalizeJsonValue(object? value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number => element.GetRawText(),
            _ => element.GetRawText(),
        };
    }

    private List<AuditEntry> BuildAuditEntries(string merchantId, Merchant before, Merchant after)
    {
        var username = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "unknown";
        var entries = new List<AuditEntry>();

        void AddIfChanged(string field, string? oldValue, string? newValue)
        {
            if (oldValue != newValue)
            {
                entries.Add(new AuditEntry
                {
                    Username = username,
                    Environment = environmentContext.Name,
                    MerchantId = merchantId,
                    Screen = "MERCHANT_DETAILS",
                    Operation = "UPDATE",
                    Entity = "Merchant",
                    Field = field,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Result = AuditResults.Success,
                    CorrelationId = CorrelationId,
                });
            }
        }

        AddIfChanged("nameEn", before.NameEn, after.NameEn);
        AddIfChanged("nameAr", before.NameAr, after.NameAr);
        AddIfChanged("brandNameEn", before.BrandNameEn, after.BrandNameEn);
        AddIfChanged("brandNameAr", before.BrandNameAr, after.BrandNameAr);
        AddIfChanged("crExpiryDate", before.CrExpiryDate, after.CrExpiryDate);
        AddIfChanged("idExpiryDate", before.IdExpiryDate, after.IdExpiryDate);

        return entries;
    }
}
