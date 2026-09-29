using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Api.Common;
using Portal.Application.Audit;
using Portal.Application.Common;
using Portal.Application.Screens;

namespace Portal.Api.Controllers;

/// <summary>
/// Backing data for the SAMPLE_SCREEN proof-of-concept (docs/15-development-roadmap.md
/// Phase 3) — demonstrates the full write-operation pattern (auth → authz →
/// validation → load → apply → audit → result, CLAUDE.md §36) end-to-end
/// before any real screen exists. Not a template to copy for real screens:
/// real screens call a real provider, not an in-memory singleton.
/// </summary>
[Authorize]
[Route("api/v1/sample-screen")]
public sealed class SampleScreenController(
    ISampleScreenService sampleScreenService,
    IValidator<SampleRecord> validator,
    IAuditService auditService,
    IEnvironmentContext environmentContext) : ApiControllerBase
{
    [HttpGet("record")]
    public async Task<IActionResult> GetRecord(CancellationToken ct)
    {
        var record = await sampleScreenService.GetRecordAsync(ct);
        return Ok(ApiResponse<SampleRecord>.Ok(record, CorrelationId));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SampleRecord record, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(record, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail(
                new ApiError
                {
                    Code = "VALIDATION_FAILED",
                    Message = "One or more fields are invalid.",
                    Details = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                },
                CorrelationId));
        }

        var before = await sampleScreenService.GetRecordAsync(ct);
        var after = await sampleScreenService.SaveAsync(record, ct);

        await auditService.WriteAsync(BuildAuditEntries(before, after), ct);

        return Ok(ApiResponse<SampleRecord>.Ok(after, CorrelationId));
    }

    private List<AuditEntry> BuildAuditEntries(SampleRecord before, SampleRecord after)
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
                    Screen = ScreenCodes.SampleScreen,
                    Operation = "SAVE",
                    Entity = "SampleRecord",
                    Field = field,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Result = AuditResults.Success,
                    CorrelationId = CorrelationId,
                });
            }
        }

        AddIfChanged("fullName", before.FullName, after.FullName);
        AddIfChanged("status", before.Status, after.Status);
        AddIfChanged("notes", before.Notes, after.Notes);

        // A no-op save (nothing actually changed) still proves the round trip happened.
        if (entries.Count == 0)
        {
            entries.Add(new AuditEntry
            {
                Username = username,
                Environment = environmentContext.Name,
                Screen = ScreenCodes.SampleScreen,
                Operation = "SAVE",
                Entity = "SampleRecord",
                Result = AuditResults.Success,
                CorrelationId = CorrelationId,
            });
        }

        return entries;
    }
}
