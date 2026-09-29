using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Common;
using Portal.Application.Customers;
using Portal.Application.Merchants;
using Portal.Application.Screens;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Screens;

/// <summary>
/// Seeds dynamic screen metadata. Idempotent; runs in every environment, same
/// as IdentitySeeder.
/// </summary>
public static class ScreenSeeder
{
    public static async Task SeedAllAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        await SeedSampleScreenAsync(dbContext, ct);
        await SeedKycScreenAsync(dbContext, ct);
        await SeedIvrScreenAsync(dbContext, ct);
        await SeedCreationScreenAsync(dbContext, ct);
        await SeedOtpScreenAsync(dbContext, ct);
        await SeedBiometricScreenAsync(dbContext, ct);
        await SeedSecurityScreenAsync(dbContext, ct);
        await SeedOnboardingScreenAsync(dbContext, ct);
        await SeedCardsScreenAsync(dbContext, ct);
        await SeedBeneficiaryScreenAsync(dbContext, ct);
        await SeedMerchantScreenAsync(dbContext, ct);
        await SeedMerchantB2BScreenAsync(dbContext, ct);
    }

    /// <summary>
    /// A generic proof of the metadata → API → DynamicForm round trip
    /// (docs/15-development-roadmap.md Phase 3), covering every supported
    /// ControlType, deliberately not tied to any real business entity.
    /// </summary>
    public static async Task SeedSampleScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, ScreenCodes.SampleScreen, "Dynamic Form Sample",
            "Phase 3 proof-of-concept screen — exercises every supported control type. Not a real business screen.",
            "Sample", ct);

        var statusOptions = JsonSerializer.Serialize(new[]
        {
            new { value = "Active", label = "Active" },
            new { value = "Inactive", label = "Inactive" },
            new { value = "Pending", label = "Pending" },
        });

        ReplaceFields(dbContext, screen,
        [
            new ScreenField { FieldKey = "recordId", Label = "Record ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "recordId" },
            new ScreenField { FieldKey = "fullName", Label = "Full Name", DataType = "Text", ControlType = "Text", Required = true, DisplayOrder = 1, IntegrationKey = "fullName" },
            new ScreenField { FieldKey = "age", Label = "Age", DataType = "Number", ControlType = "Number", DisplayOrder = 2, IntegrationKey = "age" },
            new ScreenField { FieldKey = "balance", Label = "Balance", DataType = "Decimal", ControlType = "Decimal", DisplayOrder = 3, IntegrationKey = "balance" },
            new ScreenField { FieldKey = "joinDate", Label = "Join Date", DataType = "Date", ControlType = "Date", DisplayOrder = 4, IntegrationKey = "joinDate" },
            new ScreenField { FieldKey = "isActive", Label = "Is Active", DataType = "Boolean", ControlType = "Checkbox", DisplayOrder = 5, IntegrationKey = "isActive" },
            new ScreenField { FieldKey = "status", Label = "Status", DataType = "Text", ControlType = "Select", Required = true, DisplayOrder = 6, IntegrationKey = "status", OptionsJson = statusOptions },
            new ScreenField { FieldKey = "notes", Label = "Notes", DataType = "Text", ControlType = "TextArea", DisplayOrder = 7, IntegrationKey = "notes" },
            new ScreenField { FieldKey = "internalNote", Label = "Internal Audit Note (admin only)", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 8, IntegrationKey = "internalNote", Permission = PermissionCodes.AdminUsers },
        ]);

        ReplaceActions(dbContext, screen,
        [
            new ScreenAction { Code = "SAVE", Label = "Save", RequiresConfirmation = true },
        ]);

        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Generated from KycFieldCatalog (Portal.Application.Customers) — the
    /// catalog is the single source of truth for which fields exist and what
    /// they're called; this just adds presentation metadata around it.
    /// </summary>
    public static async Task SeedKycScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_KYC", "Update KYC Details",
            "Master spec §10 direct fields. Relational sections (other_resident_*, CONTACT_PERSON_*) are not yet implemented — pending schema, see docs/05 §4.",
            "Customer", ct);

        var fields = new List<ScreenField>
        {
            new() { FieldKey = "custId", Label = "Customer ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "custId" },
            new() { FieldKey = "custNumber", Label = "Customer Number", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 1, IntegrationKey = "custNumber" },
        };

        var order = 2;
        foreach (var definition in KycFieldCatalog.Fields)
        {
            fields.Add(new ScreenField
            {
                FieldKey = definition.Key,
                Label = definition.Label,
                DataType = definition.IsBoolean ? "Boolean" : "Text",
                ControlType = definition.IsBoolean ? "Checkbox" : "Text",
                DisplayOrder = order++,
                IntegrationKey = definition.Key,
            });
        }

        fields.Add(new ScreenField
        {
            FieldKey = "pep",
            Label = "PEP (Politically Exposed Person)",
            DataType = "Boolean",
            ControlType = "ReadOnly",
            Editable = false,
            DisplayOrder = order,
            IntegrationKey = "pep",
        });

        ReplaceFields(dbContext, screen, fields);

        ReplaceActions(dbContext, screen,
        [
            new ScreenAction { Code = "SAVE", Label = "Save Changes", Permission = PermissionCodes.CustomerKycUpdate, RequiresConfirmation = true },
        ]);

        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerKycView]);

        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Zero fields/actions — generated from IvrFieldCatalog, which is
    /// deliberately empty (master spec §11: "do not invent field names", and
    /// the IVR database schema is unknown). The screen exists so
    /// GET /screens/CUSTOMER_IVR returns a valid (empty) definition rather
    /// than 404, proving the round trip is wired; IvrPage shows an
    /// informational message instead of a form until real fields exist.
    /// </summary>
    public static async Task SeedIvrScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_IVR", "Update IVR Details",
            "Pending IVR database schema — master spec §11 explicitly says not to invent field names here. See docs/05 §3.",
            "Customer", ct);

        var fields = IvrFieldCatalog.Fields.Select((definition, index) => new ScreenField
        {
            FieldKey = definition.Key,
            Label = definition.Label,
            DataType = definition.IsBoolean ? "Boolean" : "Text",
            ControlType = definition.IsBoolean ? "Checkbox" : "Text",
            DisplayOrder = index,
            IntegrationKey = definition.Key,
        }).ToList();

        ReplaceFields(dbContext, screen, fields);

        ReplaceActions(dbContext, screen, fields.Count == 0
            ? []
            : [new ScreenAction { Code = "SAVE", Label = "Save Changes", Permission = PermissionCodes.CustomerIvrUpdate, RequiresConfirmation = true }]);

        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerIvrView]);

        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Generated from CreationFieldCatalog (master spec §12) — three known
    /// date fields, two with an ambiguous column name (see the catalog's doc
    /// comment).
    /// </summary>
    public static async Task SeedCreationScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_CREATION", "Customer Creation Details",
            "Master spec §12. Two fields have an ambiguous column name in the spec itself — see CreationFieldCatalog.",
            "Customer", ct);

        var fields = new List<ScreenField>
        {
            new() { FieldKey = "custId", Label = "Customer ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "custId" },
            new() { FieldKey = "custNumber", Label = "Customer Number", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 1, IntegrationKey = "custNumber" },
        };

        var order = 2;
        foreach (var definition in CreationFieldCatalog.Fields)
        {
            fields.Add(new ScreenField
            {
                FieldKey = definition.Key,
                Label = definition.Label,
                DataType = "Date",
                ControlType = "Date",
                DisplayOrder = order++,
                IntegrationKey = definition.Key,
            });
        }

        ReplaceFields(dbContext, screen, fields);

        ReplaceActions(dbContext, screen,
        [
            new ScreenAction { Code = "SAVE", Label = "Save Changes", Permission = PermissionCodes.CustomerCreationUpdate, RequiresConfirmation = true },
        ]);

        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerCreationView]);

        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Deliberately empty — master spec §13 gives no field names for the OTP/IVR cooling period screen.</summary>
    public static async Task SeedOtpScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_OTP", "OTP / IVR Cooling Period",
            "Pending schema — master spec §13 gives no field names, only \"the backend must encapsulate the MSSQL implementation.\"",
            "Customer", ct);

        ReplaceFields(dbContext, screen, []);
        ReplaceActions(dbContext, screen, []);
        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerOtpView]);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Deliberately empty — the Biometric Authentication Module's schema is unknown (docs/05 §3).</summary>
    public static async Task SeedBiometricScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_BIOMETRIC", "Biometric Expiry",
            "Pending Biometric Authentication Module database schema — see docs/05-database-design.md §3.",
            "Customer", ct);

        ReplaceFields(dbContext, screen, []);
        ReplaceActions(dbContext, screen, []);
        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerBiometricView]);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Master spec §17 — view-only fields plus one reset action, PREPROD-sensitive (docs/04 §2).</summary>
    public static async Task SeedSecurityScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_SECURITY", "Remove Security Lock",
            "Master spec §17. Highly sensitive — confirm the fields shown before removing a lock.",
            "Customer", ct);

        ReplaceFields(dbContext, screen,
        [
            new ScreenField { FieldKey = "custId", Label = "Customer ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "custId" },
            new ScreenField { FieldKey = "custNumber", Label = "Customer Number", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 1, IntegrationKey = "custNumber" },
            new ScreenField { FieldKey = "failedLogonCount", Label = "Failed Logon Count", DataType = "Number", ControlType = "ReadOnly", Editable = false, DisplayOrder = 2, IntegrationKey = "failedLogonCount" },
            new ScreenField { FieldKey = "failedOtpCount", Label = "Failed OTP Count", DataType = "Number", ControlType = "ReadOnly", Editable = false, DisplayOrder = 3, IntegrationKey = "failedOtpCount" },
            new ScreenField { FieldKey = "currentOtpStatus", Label = "Current OTP Status", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 4, IntegrationKey = "currentOtpStatus" },
            new ScreenField { FieldKey = "freezeStatusId", Label = "Freeze Status ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 5, IntegrationKey = "freezeStatusId" },
        ]);

        ReplaceActions(dbContext, screen,
        [
            new ScreenAction { Code = "REMOVE_LOCK", Label = "Remove Security Lock", Permission = PermissionCodes.CustomerSecurityRemove, RequiresConfirmation = true },
        ]);

        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerSecurityView]);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Master spec §21 — read-only.</summary>
    public static async Task SeedOnboardingScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_ONBOARDING", "Onboarding",
            "Master spec §21 — read-only. Do not add update operations without an explicit decision to do so.",
            "Customer", ct);

        ReplaceFields(dbContext, screen,
        [
            new ScreenField { FieldKey = "custId", Label = "Customer ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "custId" },
            new ScreenField { FieldKey = "custNumber", Label = "Customer Number", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 1, IntegrationKey = "custNumber" },
            new ScreenField { FieldKey = "t24CustomerId", Label = "T24 Customer ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 2, IntegrationKey = "t24CustomerId" },
            new ScreenField { FieldKey = "dateCreated", Label = "Created Date", DataType = "Date", ControlType = "ReadOnly", Editable = false, DisplayOrder = 3, IntegrationKey = "dateCreated" },
        ]);

        ReplaceActions(dbContext, screen, []);
        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerOnboardingView]);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Master spec §14 — status view plus an activate action; CreateCard runs implicitly if no card exists yet.</summary>
    public static async Task SeedCardsScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_CARDS", "Card Activation",
            "Master spec §14, via ICardManagementClient (ASSUMED CONTRACT — docs/08 §4).",
            "Customer", ct);

        ReplaceFields(dbContext, screen,
        [
            new ScreenField { FieldKey = "cardNumber", Label = "Card Number", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "cardNumber" },
            new ScreenField { FieldKey = "status", Label = "Status", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 1, IntegrationKey = "status" },
        ]);

        ReplaceActions(dbContext, screen,
        [
            new ScreenAction { Code = "ACTIVATE_CARD", Label = "Activate Card", Permission = PermissionCodes.CustomerCardActivate, RequiresConfirmation = true },
        ]);

        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerCardView]);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Master spec §15 — status view plus an activate action, via IBeneficiaryServiceClient (ASSUMED CONTRACT — docs/08 §4).</summary>
    public static async Task SeedBeneficiaryScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "CUSTOMER_BENEFICIARY", "Beneficiary Activation",
            "Master spec §15, via IBeneficiaryServiceClient (ASSUMED CONTRACT — docs/08 §4).",
            "Customer", ct);

        ReplaceFields(dbContext, screen,
        [
            new ScreenField { FieldKey = "beneficiaryId", Label = "Beneficiary ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "beneficiaryId" },
            new ScreenField { FieldKey = "status", Label = "Status", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 1, IntegrationKey = "status" },
        ]);

        ReplaceActions(dbContext, screen,
        [
            new ScreenAction { Code = "ACTIVATE_BENEFICIARY", Label = "Activate Beneficiary", Permission = PermissionCodes.CustomerBeneficiaryActivate, RequiresConfirmation = true },
        ]);

        ReplacePermissions(dbContext, screen, [PermissionCodes.CustomerBeneficiaryView]);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Generated from MerchantFieldCatalog (master spec §16).</summary>
    public static async Task SeedMerchantScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "MERCHANT_DETAILS", "Merchant Details",
            "Master spec §16.", "Merchant", ct);

        var fields = new List<ScreenField>
        {
            new() { FieldKey = "merchantId", Label = "Merchant ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "merchantId" },
            new() { FieldKey = "merchantNumber", Label = "Merchant Number", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 1, IntegrationKey = "merchantNumber" },
        };

        var order = 2;
        foreach (var definition in MerchantFieldCatalog.Fields)
        {
            fields.Add(new ScreenField
            {
                FieldKey = definition.Key,
                Label = definition.Label,
                DataType = "Text",
                ControlType = "Text",
                DisplayOrder = order++,
                IntegrationKey = definition.Key,
            });
        }

        ReplaceFields(dbContext, screen, fields);

        ReplaceActions(dbContext, screen,
        [
            new ScreenAction { Code = "SAVE", Label = "Save Changes", Permission = PermissionCodes.MerchantUpdate, RequiresConfirmation = true },
        ]);

        ReplacePermissions(dbContext, screen, [PermissionCodes.MerchantView]);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Master spec §20 — status view plus an "add to B2B" action, via IB2BServiceClient (ASSUMED CONTRACT — docs/08 §4).</summary>
    public static async Task SeedMerchantB2BScreenAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        var screen = await GetOrCreateScreenAsync(
            dbContext, "MERCHANT_B2B", "Add Merchant to B2B",
            "Master spec §20, via IB2BServiceClient (ASSUMED CONTRACT — docs/08 §4).", "Merchant", ct);

        ReplaceFields(dbContext, screen,
        [
            new ScreenField { FieldKey = "merchantId", Label = "Merchant ID", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 0, IntegrationKey = "merchantId" },
            new ScreenField { FieldKey = "status", Label = "B2B Status", DataType = "Text", ControlType = "ReadOnly", Editable = false, DisplayOrder = 1, IntegrationKey = "status" },
        ]);

        ReplaceActions(dbContext, screen,
        [
            new ScreenAction { Code = "ADD_TO_B2B", Label = "Add to B2B", Permission = PermissionCodes.MerchantB2BUpdate, RequiresConfirmation = true },
        ]);

        ReplacePermissions(dbContext, screen, [PermissionCodes.MerchantB2BView]);
        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task<Screen> GetOrCreateScreenAsync(
        PortalDbContext dbContext, string code, string name, string description, string category, CancellationToken ct)
    {
        var screen = await dbContext.Screens
            .Include(s => s.Fields)
            .Include(s => s.Actions)
            .Include(s => s.Permissions)
            .SingleOrDefaultAsync(s => s.Code == code, ct);

        if (screen is not null)
        {
            return screen;
        }

        screen = new Screen
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Category = category,
            IsActive = true,
            DisplayOrder = 0,
        };
        dbContext.Screens.Add(screen);
        return screen;
    }

    private static void ReplaceFields(PortalDbContext dbContext, Screen screen, List<ScreenField> desired)
    {
        dbContext.ScreenFields.RemoveRange(screen.Fields);
        foreach (var field in desired)
        {
            field.Id = Guid.NewGuid();
            field.ScreenId = screen.Id;
        }
        dbContext.ScreenFields.AddRange(desired);
    }

    private static void ReplaceActions(PortalDbContext dbContext, Screen screen, List<ScreenAction> desired)
    {
        dbContext.ScreenActions.RemoveRange(screen.Actions);
        foreach (var action in desired)
        {
            action.Id = Guid.NewGuid();
            action.ScreenId = screen.Id;
        }
        dbContext.ScreenActions.AddRange(desired);
    }

    private static void ReplacePermissions(PortalDbContext dbContext, Screen screen, List<string> permissions)
    {
        dbContext.ScreenPermissions.RemoveRange(screen.Permissions);
        dbContext.ScreenPermissions.AddRange(permissions.Select(p => new ScreenPermission
        {
            Id = Guid.NewGuid(),
            ScreenId = screen.Id,
            Permission = p,
        }));
    }
}
