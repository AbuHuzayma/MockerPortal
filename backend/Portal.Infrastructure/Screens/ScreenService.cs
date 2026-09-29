using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Common;
using Portal.Application.Screens;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Screens;

public sealed class ScreenService(PortalDbContext dbContext) : IScreenService
{
    public async Task<Result<ScreenDefinitionDto>> GetScreenDefinitionAsync(
        string code,
        IReadOnlySet<string> callerPermissions,
        CancellationToken ct)
    {
        var screen = await dbContext.Screens
            .Include(s => s.Fields)
            .Include(s => s.Actions)
            .Include(s => s.Permissions)
            .SingleOrDefaultAsync(s => s.Code == code && s.IsActive, ct);

        if (screen is null)
        {
            return Result<ScreenDefinitionDto>.Failure(ScreenErrorCodes.ScreenNotFound, "Screen was not found.");
        }

        // Screen-level gate: the caller needs every listed permission to view the screen at all.
        if (screen.Permissions.Any(p => !callerPermissions.Contains(p.Permission)))
        {
            return Result<ScreenDefinitionDto>.Failure(ScreenErrorCodes.ScreenForbidden, "You do not have permission to view this screen.");
        }

        var fields = screen.Fields
            .Where(f => f.Visible && (f.Permission is null || callerPermissions.Contains(f.Permission)))
            .OrderBy(f => f.DisplayOrder)
            .Select(ToDto)
            .ToList();

        var actions = screen.Actions
            .Where(a => a.Permission is null || callerPermissions.Contains(a.Permission))
            .Select(a => new ScreenActionDto(a.Code, a.Label, a.RequiresConfirmation))
            .ToList();

        var definition = new ScreenDefinitionDto(screen.Code, screen.Name, screen.Description, fields, actions);
        return Result<ScreenDefinitionDto>.Success(definition);
    }

    public async Task<IReadOnlyList<ScreenAdminDto>> ListAllAsync(CancellationToken ct)
    {
        var screens = await dbContext.Screens
            .Include(s => s.Fields)
            .Include(s => s.Actions)
            .Include(s => s.Permissions)
            .OrderBy(s => s.Category).ThenBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        return screens.Select(screen => new ScreenAdminDto(
            screen.Code,
            screen.Name,
            screen.Description,
            screen.Category,
            screen.IsActive,
            screen.DisplayOrder,
            screen.Permissions.Select(p => p.Permission).ToList(),
            screen.Fields.OrderBy(f => f.DisplayOrder).Select(ToDto).ToList(),
            screen.Actions.Select(a => new ScreenActionDto(a.Code, a.Label, a.RequiresConfirmation)).ToList()
        )).ToList();
    }

    private static readonly JsonSerializerOptions OptionsJsonSettings = new() { PropertyNameCaseInsensitive = true };

    private static ScreenFieldDto ToDto(ScreenField field)
    {
        var options = field.OptionsJson is null
            ? null
            : JsonSerializer.Deserialize<List<ScreenFieldOptionDto>>(field.OptionsJson, OptionsJsonSettings);

        return new ScreenFieldDto(
            field.FieldKey,
            field.Label,
            field.DataType,
            field.ControlType,
            field.Required,
            field.Editable,
            field.DisplayOrder,
            field.IntegrationKey,
            options);
    }
}
