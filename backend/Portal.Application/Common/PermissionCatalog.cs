namespace Portal.Application.Common;

public sealed record PermissionDefinition(string Code, string Description, string Category);

/// <summary>
/// The authoritative permission catalog, seeded into the Permissions table.
/// Adding a permission here is a code change (reviewed), not an admin-UI action —
/// see docs/07-dynamic-screen-engine.md §6 for why that boundary matters.
/// </summary>
public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(PermissionCodes.CustomerView, "View customer profile", "Customer"),
        new(PermissionCodes.CustomerKycView, "View KYC details", "Customer"),
        new(PermissionCodes.CustomerKycUpdate, "Update KYC details", "Customer"),
        new(PermissionCodes.CustomerKycUpdateQa, "Update KYC details in QA", "Customer"),
        new(PermissionCodes.CustomerKycUpdatePreprod, "Update KYC details in PREPROD", "Customer"),
        new(PermissionCodes.CustomerIvrView, "View IVR details", "Customer"),
        new(PermissionCodes.CustomerIvrUpdate, "Update IVR details", "Customer"),
        new(PermissionCodes.CustomerCreationView, "View customer creation details", "Customer"),
        new(PermissionCodes.CustomerCreationUpdate, "Update customer creation details", "Customer"),
        new(PermissionCodes.CustomerOtpView, "View OTP/IVR cooling period", "Customer"),
        new(PermissionCodes.CustomerOtpUpdate, "Update OTP/IVR cooling period", "Customer"),
        new(PermissionCodes.CustomerCardView, "View card status", "Customer"),
        new(PermissionCodes.CustomerCardActivate, "Activate/create cards", "Customer"),
        new(PermissionCodes.CustomerBeneficiaryView, "View beneficiaries", "Customer"),
        new(PermissionCodes.CustomerBeneficiaryActivate, "Activate internal transfer beneficiaries", "Customer"),
        new(PermissionCodes.CustomerSecurityView, "View security lock status", "Customer"),
        new(PermissionCodes.CustomerSecurityRemove, "Remove customer security locks", "Customer"),
        new(PermissionCodes.CustomerSecurityRemovePreprod, "Remove customer security locks in PREPROD", "Customer"),
        new(PermissionCodes.CustomerBiometricView, "View biometric expiry", "Customer"),
        new(PermissionCodes.CustomerBiometricUpdate, "Update biometric expiry", "Customer"),
        new(PermissionCodes.CustomerOnboardingView, "View onboarding information", "Customer"),

        new(PermissionCodes.MerchantView, "View merchant details", "Merchant"),
        new(PermissionCodes.MerchantUpdate, "Update merchant details", "Merchant"),
        new(PermissionCodes.MerchantB2BView, "View B2B subscriptions", "Merchant"),
        new(PermissionCodes.MerchantB2BUpdate, "Add merchant to B2B services", "Merchant"),

        new(PermissionCodes.ApiMockerView, "View API Mocker configuration", "ApiMocker"),
        new(PermissionCodes.ApiMockerManage, "Manage API Mocker configuration", "ApiMocker"),
        new(PermissionCodes.ApiMockerEnable, "Enable API mocks", "ApiMocker"),
        new(PermissionCodes.ApiMockerEnablePreprod, "Enable API mocks in PREPROD", "ApiMocker"),
        new(PermissionCodes.ApiMockerDisable, "Disable API mocks", "ApiMocker"),

        new(PermissionCodes.AuditView, "View audit log", "Audit"),

        new(PermissionCodes.AdminUsers, "Manage users", "Admin"),
        new(PermissionCodes.AdminRoles, "Manage roles", "Admin"),
        new(PermissionCodes.AdminPermissions, "Manage permissions", "Admin"),
        new(PermissionCodes.AdminScreens, "Manage dynamic screen metadata", "Admin"),
        new(PermissionCodes.AdminIntegrations, "Manage integration configuration", "Admin"),
        new(PermissionCodes.AdminMockConfig, "Manage API mock configuration (admin)", "Admin"),
    ];
}
