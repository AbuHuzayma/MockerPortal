namespace Portal.Application.Common;

/// <summary>
/// Compile-time-safe constants for every permission code. The authoritative set —
/// including description/category, used to seed the Permissions table — is
/// <see cref="PermissionCatalog"/>. See docs/04-authentication-authorization.md §2.
/// </summary>
public static class PermissionCodes
{
    // Customer
    public const string CustomerView = "customer.view";
    public const string CustomerKycView = "customer.kyc.view";
    public const string CustomerKycUpdate = "customer.kyc.update";
    public const string CustomerKycUpdateQa = "customer.kyc.update.qa";
    public const string CustomerKycUpdatePreprod = "customer.kyc.update.preprod";
    public const string CustomerIvrView = "customer.ivr.view";
    public const string CustomerIvrUpdate = "customer.ivr.update";
    public const string CustomerCreationView = "customer.creation.view";
    public const string CustomerCreationUpdate = "customer.creation.update";
    public const string CustomerOtpView = "customer.otp.view";
    public const string CustomerOtpUpdate = "customer.otp.update";
    public const string CustomerCardView = "customer.card.view";
    public const string CustomerCardActivate = "customer.card.activate";
    public const string CustomerBeneficiaryView = "customer.beneficiary.view";
    public const string CustomerBeneficiaryActivate = "customer.beneficiary.activate";
    public const string CustomerSecurityView = "customer.security.view";
    public const string CustomerSecurityRemove = "customer.security.remove";
    public const string CustomerSecurityRemovePreprod = "customer.security.remove.preprod";
    public const string CustomerBiometricView = "customer.biometric.view";
    public const string CustomerBiometricUpdate = "customer.biometric.update";
    public const string CustomerOnboardingView = "customer.onboarding.view";

    // Merchant
    public const string MerchantView = "merchant.view";
    public const string MerchantUpdate = "merchant.update";
    public const string MerchantB2BView = "merchant.b2b.view";
    public const string MerchantB2BUpdate = "merchant.b2b.update";

    // API Mocker
    public const string ApiMockerView = "api-mocker.view";
    public const string ApiMockerManage = "api-mocker.manage";
    public const string ApiMockerEnable = "api-mocker.enable";
    public const string ApiMockerEnablePreprod = "api-mocker.enable.preprod";
    public const string ApiMockerDisable = "api-mocker.disable";

    // Audit
    public const string AuditView = "audit.view";

    // Admin
    public const string AdminUsers = "admin.users";
    public const string AdminRoles = "admin.roles";
    public const string AdminPermissions = "admin.permissions";
    public const string AdminScreens = "admin.screens";
    public const string AdminIntegrations = "admin.integrations";
    public const string AdminMockConfig = "admin.mock-config";
}
