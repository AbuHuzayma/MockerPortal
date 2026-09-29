using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Portal.Api.Authorization;
using Portal.Api.Common;
using Portal.Application.Audit;
using Portal.Application.Common;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Api.Controllers;

[HasPermission(PermissionCodes.CustomerView)]
public sealed class CustomersController(
    ICustomerService customerService,
    IKycService kycService,
    IIvrService ivrService,
    ICreationService creationService,
    IOtpCoolingService otpCoolingService,
    IBiometricService biometricService,
    ISecurityLockService securityLockService,
    IOnboardingService onboardingService,
    ICardManagementClient cardManagementClient,
    IBeneficiaryServiceClient beneficiaryServiceClient,
    IValidator<SearchCustomerRequest> searchValidator,
    IValidator<KycFieldValues> kycValidator,
    IValidator<CreationFieldValues> creationValidator,
    IAuditService auditService,
    IEnvironmentContext environmentContext) : ApiControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? mobileNumber, CancellationToken ct)
    {
        var request = new SearchCustomerRequest(mobileNumber ?? string.Empty);
        var validation = await searchValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationFailed(validation);
        }

        var result = await customerService.SearchByMobileNumberAsync(request.MobileNumber, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.Value, CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetProfile(string customerId, CancellationToken ct)
    {
        var result = await customerService.GetProfileAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.Value, CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerKycView)]
    [HttpGet("{customerId}/kyc")]
    public async Task<IActionResult> GetKyc(string customerId, CancellationToken ct)
    {
        var result = await kycService.GetKycAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(ToPayload(result.Value!.CustId, result.Value.CustNumber, result.Value.Fields), CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerKycUpdate, environmentScoped: true)]
    [HttpPut("{customerId}/kyc")]
    public async Task<IActionResult> UpdateKyc(string customerId, [FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        var normalized = NormalizeBody(body);

        var validation = await kycValidator.ValidateAsync(new KycFieldValues(normalized), ct);
        if (!validation.IsValid)
        {
            return ValidationFailed(validation);
        }

        var result = await kycService.UpdateKycAsync(customerId, normalized, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await auditService.WriteAsync(
            BuildAuditEntries(customerId, "CUSTOMER_KYC", "Kyc", KycFieldCatalog.ValidKeys, result.Value!.Before.Fields, result.Value.After.Fields,
                key => key == KycFieldCatalog.MobileNumberKey),
            ct);

        return Ok(ApiResponse<object>.Ok(ToPayload(result.Value.After.CustId, result.Value.After.CustNumber, result.Value.After.Fields), CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerIvrView)]
    [HttpGet("{customerId}/ivr")]
    public async Task<IActionResult> GetIvr(string customerId, CancellationToken ct)
    {
        var result = await ivrService.GetIvrAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(ToPayload(result.Value!.CustId, result.Value.CustNumber, result.Value.Fields), CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerIvrUpdate)]
    [HttpPut("{customerId}/ivr")]
    public async Task<IActionResult> UpdateIvr(string customerId, [FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        var normalized = NormalizeBody(body);

        var result = await ivrService.UpdateIvrAsync(customerId, normalized, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await auditService.WriteAsync(
            BuildAuditEntries(customerId, "CUSTOMER_IVR", "Ivr", IvrFieldCatalog.ValidKeys, result.Value!.Before.Fields, result.Value.After.Fields, _ => false),
            ct);

        return Ok(ApiResponse<object>.Ok(ToPayload(result.Value.After.CustId, result.Value.After.CustNumber, result.Value.After.Fields), CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerCreationView)]
    [HttpGet("{customerId}/creation")]
    public async Task<IActionResult> GetCreation(string customerId, CancellationToken ct)
    {
        var result = await creationService.GetCreationAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(ToPayload(result.Value!.CustId, result.Value.CustNumber, result.Value.Fields), CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerCreationUpdate)]
    [HttpPut("{customerId}/creation")]
    public async Task<IActionResult> UpdateCreation(string customerId, [FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        var normalized = NormalizeBody(body);

        var validation = await creationValidator.ValidateAsync(new CreationFieldValues(normalized), ct);
        if (!validation.IsValid)
        {
            return ValidationFailed(validation);
        }

        var result = await creationService.UpdateCreationAsync(customerId, normalized, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await auditService.WriteAsync(
            BuildAuditEntries(customerId, "CUSTOMER_CREATION", "Creation", CreationFieldCatalog.ValidKeys, result.Value!.Before.Fields, result.Value.After.Fields, _ => false),
            ct);

        return Ok(ApiResponse<object>.Ok(ToPayload(result.Value.After.CustId, result.Value.After.CustNumber, result.Value.After.Fields), CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerOtpView)]
    [HttpGet("{customerId}/otp-cooling")]
    public async Task<IActionResult> GetOtpCooling(string customerId, CancellationToken ct)
    {
        var result = await otpCoolingService.GetOtpCoolingAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(ToPayload(result.Value!.CustId, result.Value.CustNumber, result.Value.Fields), CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerOtpUpdate)]
    [HttpPut("{customerId}/otp-cooling")]
    public async Task<IActionResult> UpdateOtpCooling(string customerId, [FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        var normalized = NormalizeBody(body);

        var result = await otpCoolingService.UpdateOtpCoolingAsync(customerId, normalized, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await auditService.WriteAsync(
            BuildAuditEntries(customerId, "CUSTOMER_OTP", "OtpCooling", OtpCoolingFieldCatalog.ValidKeys, result.Value!.Before.Fields, result.Value.After.Fields, _ => false),
            ct);

        return Ok(ApiResponse<object>.Ok(ToPayload(result.Value.After.CustId, result.Value.After.CustNumber, result.Value.After.Fields), CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerBiometricView)]
    [HttpGet("{customerId}/biometrics")]
    public async Task<IActionResult> GetBiometric(string customerId, CancellationToken ct)
    {
        var result = await biometricService.GetBiometricAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(ToPayload(result.Value!.CustId, result.Value.CustNumber, result.Value.Fields), CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerBiometricUpdate)]
    [HttpPut("{customerId}/biometrics")]
    public async Task<IActionResult> UpdateBiometric(string customerId, [FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        var normalized = NormalizeBody(body);

        var result = await biometricService.UpdateBiometricAsync(customerId, normalized, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        await auditService.WriteAsync(
            BuildAuditEntries(customerId, "CUSTOMER_BIOMETRIC", "Biometric", BiometricFieldCatalog.ValidKeys, result.Value!.Before.Fields, result.Value.After.Fields, _ => false),
            ct);

        return Ok(ApiResponse<object>.Ok(ToPayload(result.Value.After.CustId, result.Value.After.CustNumber, result.Value.After.Fields), CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerSecurityView)]
    [HttpGet("{customerId}/security")]
    public async Task<IActionResult> GetSecurityStatus(string customerId, CancellationToken ct)
    {
        var result = await securityLockService.GetStatusAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(ToSecurityPayload(result.Value!), CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerSecurityRemove, environmentScoped: true)]
    [HttpPost("{customerId}/security/remove-lock")]
    public async Task<IActionResult> RemoveSecurityLock(string customerId, CancellationToken ct)
    {
        var result = await securityLockService.RemoveLockAsync(customerId, ct);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
        }

        var before = result.Value!.Before;
        var after = result.Value.After;
        var username = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "unknown";
        var entries = new List<AuditEntry>();
        void AddIfChanged(string field, object? oldValue, object? newValue)
        {
            if (!Equals(oldValue, newValue))
            {
                entries.Add(new AuditEntry
                {
                    Username = username,
                    Environment = environmentContext.Name,
                    CustomerId = customerId,
                    Screen = "CUSTOMER_SECURITY",
                    Operation = "REMOVE_LOCK",
                    Entity = "SecurityLock",
                    Field = field,
                    OldValue = oldValue?.ToString(),
                    NewValue = newValue?.ToString(),
                    Result = AuditResults.Success,
                    CorrelationId = CorrelationId,
                });
            }
        }
        AddIfChanged("failedLogonCount", before.FailedLogonCount, after.FailedLogonCount);
        AddIfChanged("failedOtpCount", before.FailedOtpCount, after.FailedOtpCount);
        AddIfChanged("currentOtpStatus", before.CurrentOtpStatus, after.CurrentOtpStatus);
        AddIfChanged("freezeStatusId", before.FreezeStatusId, after.FreezeStatusId);
        await auditService.WriteAsync(entries, ct);

        return Ok(ApiResponse<object>.Ok(ToSecurityPayload(after), CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerOnboardingView)]
    [HttpGet("{customerId}/onboarding")]
    public async Task<IActionResult> GetOnboarding(string customerId, CancellationToken ct)
    {
        var result = await onboardingService.GetOnboardingAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(result.Value, CorrelationId))
            : NotFound(ApiResponse<object>.Fail(new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! }, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerCardView)]
    [HttpGet("{customerId}/cards")]
    public async Task<IActionResult> GetCardStatus(string customerId, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await cardManagementClient.GetStatusAsync(customerId, ct), CorrelationId));

    [HasPermission(PermissionCodes.CustomerCardActivate)]
    [HttpPost("{customerId}/cards/activate")]
    public async Task<IActionResult> ActivateCard(string customerId, CancellationToken ct)
    {
        var before = await cardManagementClient.GetStatusAsync(customerId, ct);
        if (before.Status == "None")
        {
            await cardManagementClient.CreateCardAsync(customerId, ct);
        }
        var after = await cardManagementClient.ActivateCardAsync(customerId, ct);

        await auditService.WriteAsync([new AuditEntry
        {
            Username = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "unknown",
            Environment = environmentContext.Name,
            CustomerId = customerId,
            Screen = "CUSTOMER_CARDS",
            Operation = "ACTIVATE_CARD",
            Entity = "Card",
            Field = "status",
            OldValue = before.Status,
            NewValue = after.Status,
            Result = AuditResults.Success,
            CorrelationId = CorrelationId,
        }], ct);

        return Ok(ApiResponse<object>.Ok(after, CorrelationId));
    }

    [HasPermission(PermissionCodes.CustomerBeneficiaryView)]
    [HttpGet("{customerId}/beneficiaries/{beneficiaryId}")]
    public async Task<IActionResult> GetBeneficiaryStatus(string customerId, string beneficiaryId, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await beneficiaryServiceClient.GetStatusAsync(customerId, beneficiaryId, ct), CorrelationId));

    [HasPermission(PermissionCodes.CustomerBeneficiaryActivate)]
    [HttpPost("{customerId}/beneficiaries/{beneficiaryId}/activate")]
    public async Task<IActionResult> ActivateBeneficiary(string customerId, string beneficiaryId, CancellationToken ct)
    {
        var before = await beneficiaryServiceClient.GetStatusAsync(customerId, beneficiaryId, ct);
        var after = await beneficiaryServiceClient.ActivateBeneficiaryAsync(customerId, beneficiaryId, ct);

        await auditService.WriteAsync([new AuditEntry
        {
            Username = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "unknown",
            Environment = environmentContext.Name,
            CustomerId = customerId,
            Screen = "CUSTOMER_BENEFICIARY",
            Operation = "ACTIVATE_BENEFICIARY",
            Entity = "Beneficiary",
            Field = "status",
            OldValue = before.Status,
            NewValue = after.Status,
            Result = AuditResults.Success,
            CorrelationId = CorrelationId,
        }], ct);

        return Ok(ApiResponse<object>.Ok(after, CorrelationId));
    }

    private static object ToSecurityPayload(SecurityLockStatus status) => new
    {
        custId = status.CustId,
        custNumber = status.CustNumber,
        failedLogonCount = status.FailedLogonCount,
        failedOtpCount = status.FailedOtpCount,
        currentOtpStatus = status.CurrentOtpStatus,
        freezeStatusId = status.FreezeStatusId,
    };

    private BadRequestObjectResult ValidationFailed(FluentValidation.Results.ValidationResult validation) =>
        BadRequest(ApiResponse<object>.Fail(
            new ApiError
            {
                Code = "VALIDATION_FAILED",
                Message = "One or more fields are invalid.",
                Details = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            },
            CorrelationId));

    private static Dictionary<string, object?> NormalizeBody(Dictionary<string, object?> body) =>
        body.ToDictionary(kvp => kvp.Key, kvp => NormalizeJsonValue(kvp.Value));

    /// <summary>System.Text.Json binds Dictionary&lt;string, object?&gt; values as JsonElement — unwrap to plain CLR values.</summary>
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

    /// <summary>
    /// Flat dictionary (custId/custNumber alongside every field) so the response
    /// can be handed straight to DynamicForm as its `values` prop — each key
    /// lines up 1:1 with a ScreenField.IntegrationKey.
    /// </summary>
    private static object ToPayload(string custId, string custNumber, IReadOnlyDictionary<string, object?> fields) =>
        new Dictionary<string, object?>(fields)
        {
            ["custId"] = custId,
            ["custNumber"] = custNumber,
        };

    private List<AuditEntry> BuildAuditEntries(
        string customerId,
        string screen,
        string entity,
        IReadOnlySet<string> validKeys,
        IReadOnlyDictionary<string, object?> before,
        IReadOnlyDictionary<string, object?> after,
        Func<string, bool> isMobileNumberField)
    {
        var username = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "unknown";
        var entries = new List<AuditEntry>();

        foreach (var key in validKeys)
        {
            before.TryGetValue(key, out var oldValue);
            after.TryGetValue(key, out var newValue);

            var oldText = FormatForAudit(oldValue, isMobileNumberField(key));
            var newText = FormatForAudit(newValue, isMobileNumberField(key));
            if (oldText == newText)
            {
                continue;
            }

            entries.Add(new AuditEntry
            {
                Username = username,
                Environment = environmentContext.Name,
                CustomerId = customerId,
                Screen = screen,
                Operation = "UPDATE",
                Entity = entity,
                Field = key,
                OldValue = oldText,
                NewValue = newText,
                Result = AuditResults.Success,
                CorrelationId = CorrelationId,
            });
        }

        return entries;
    }

    /// <summary>Masks mobile-number-like fields per docs/10-audit.md §4 (last 4 digits shown).</summary>
    private static string? FormatForAudit(object? value, bool isMobileNumberField)
    {
        if (value is null)
        {
            return null;
        }

        var text = value is bool b ? (b ? "true" : "false") : value.ToString();

        if (isMobileNumberField && !string.IsNullOrEmpty(text))
        {
            return text.Length <= 4 ? text : new string('*', text.Length - 4) + text[^4..];
        }

        return text;
    }
}
