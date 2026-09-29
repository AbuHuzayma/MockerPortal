using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portal.Application.Admin;
using Portal.Application.Audit;
using Portal.Application.Auth;
using Portal.Application.Common;
using Portal.Application.Customers;
using Portal.Application.Merchants;
using Portal.Application.Mocking;
using Portal.Application.Screens;
using Portal.Infrastructure.Audit;
using Portal.Infrastructure.Common;
using Portal.Infrastructure.Customers;
using Portal.Infrastructure.Identity;
using Portal.Infrastructure.Merchants;
using Portal.Infrastructure.Mocking;
using Portal.Infrastructure.Persistence;
using Portal.Infrastructure.Screens;

namespace Portal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEnvironmentContext, EnvironmentContext>();

        // "InMemory" exists only so the API test host (PortalApiFactory) can avoid
        // ever calling UseNpgsql — EF Core does not support two relational/provider
        // registrations coexisting in one DI container, so swapping providers via
        // ConfigureServices after the fact is unreliable. Real runs always use
        // Postgres; see docs/14-testing-strategy.md.
        var useInMemoryDatabase = configuration.GetValue<bool>("Database:UseInMemoryProvider");
        services.AddDbContext<PortalDbContext>(options =>
        {
            if (useInMemoryDatabase)
            {
                options.UseInMemoryDatabase(configuration["Database:InMemoryName"] ?? "PortalInMemory");
            }
            else
            {
                options.UseNpgsql(configuration.GetConnectionString("PortalDb"));
            }
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                // See docs/04-authentication-authorization.md §1.
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<PortalDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IOperationLogService, OperationLogService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IAdminUserService, AdminUserService>();

        var customerProviderMode = configuration["Providers:Customer:Mode"] ?? "Mock";
        if (string.Equals(customerProviderMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            var customerConnectionString = configuration.GetConnectionString("CustomerDatabase")
                ?? throw new InvalidOperationException(
                    "Providers:Customer:Mode is \"Sql\" but ConnectionStrings:CustomerDatabase is not configured.");
            services.AddScoped<ICustomerProvider>(_ => new SqlCustomerProvider(customerConnectionString));
        }
        else
        {
            services.AddSingleton<ICustomerProvider, MockCustomerProvider>();
        }

        services.AddScoped<ICustomerService, CustomerService>();

        var kycProviderMode = configuration["Providers:Kyc:Mode"] ?? "Mock";
        if (string.Equals(kycProviderMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            var kycConnectionString = configuration.GetConnectionString("CustomerDatabase")
                ?? throw new InvalidOperationException(
                    "Providers:Kyc:Mode is \"Sql\" but ConnectionStrings:CustomerDatabase is not configured.");
            services.AddScoped<IKycProvider>(_ => new SqlKycProvider(kycConnectionString));
        }
        else
        {
            services.AddSingleton<IKycProvider, MockKycProvider>();
        }

        services.AddScoped<IKycService, KycService>();

        // No SqlIvrProvider exists — the IVR database schema is unknown (master
        // spec §11 explicitly says not to invent field names), so "Sql" mode
        // fails fast rather than silently falling back to Mock.
        var ivrProviderMode = configuration["Providers:Ivr:Mode"] ?? "Mock";
        if (string.Equals(ivrProviderMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Providers:Ivr:Mode is \"Sql\" but no SqlIvrProvider exists yet — the IVR database schema has not been supplied. See docs/05-database-design.md §3.");
        }
        services.AddSingleton<IIvrProvider, MockIvrProvider>();
        services.AddScoped<IIvrService, IvrService>();

        var creationProviderMode = configuration["Providers:Creation:Mode"] ?? "Mock";
        if (string.Equals(creationProviderMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            var creationConnectionString = configuration.GetConnectionString("CustomerDatabase")
                ?? throw new InvalidOperationException(
                    "Providers:Creation:Mode is \"Sql\" but ConnectionStrings:CustomerDatabase is not configured.");
            services.AddScoped<ICreationProvider>(_ => new SqlCreationProvider(creationConnectionString));
        }
        else
        {
            services.AddSingleton<ICreationProvider, MockCreationProvider>();
        }

        services.AddScoped<ICreationService, CreationService>();

        // No SqlOtpCoolingProvider / SqlBiometricProvider exists — both database
        // schemas are unknown (master spec §13/§19, docs/05 §3), so "Sql" mode
        // fails fast rather than guessing.
        var otpProviderMode = configuration["Providers:Otp:Mode"] ?? "Mock";
        if (string.Equals(otpProviderMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Providers:Otp:Mode is \"Sql\" but no SqlOtpCoolingProvider exists yet — the schema has not been supplied. See docs/05-database-design.md §3.");
        }
        services.AddSingleton<IOtpCoolingProvider, MockOtpCoolingProvider>();
        services.AddScoped<IOtpCoolingService, OtpCoolingService>();

        var biometricProviderMode = configuration["Providers:Biometric:Mode"] ?? "Mock";
        if (string.Equals(biometricProviderMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Providers:Biometric:Mode is \"Sql\" but no SqlBiometricProvider exists yet — the schema has not been supplied. See docs/05-database-design.md §3.");
        }
        services.AddSingleton<IBiometricProvider, MockBiometricProvider>();
        services.AddScoped<IBiometricService, BiometricService>();

        var securityLockProviderMode = configuration["Providers:Customer:Mode"] ?? "Mock";
        if (string.Equals(securityLockProviderMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            var securityLockConnectionString = configuration.GetConnectionString("CustomerDatabase")
                ?? throw new InvalidOperationException(
                    "Providers:Customer:Mode is \"Sql\" but ConnectionStrings:CustomerDatabase is not configured.");
            services.AddScoped<ISecurityLockProvider>(_ => new SqlSecurityLockProvider(securityLockConnectionString));
        }
        else
        {
            services.AddSingleton<ISecurityLockProvider, MockSecurityLockProvider>();
        }

        services.AddScoped<ISecurityLockService, SecurityLockService>();

        services.AddScoped<IOnboardingService, OnboardingService>();

        // "Real" mode for Card/Beneficiary is registered in Portal.Api's
        // Program.cs, not here — Infrastructure must not reference
        // Portal.Integrations (docs/01-architecture.md's dependency rule); only
        // Api is allowed to know about concrete Integrations implementations.
        if (!string.Equals(configuration["Providers:CardManagement:Mode"] ?? "Mock", "Real", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICardManagementClient, MockCardManagementClient>();
        }

        if (!string.Equals(configuration["Providers:BeneficiaryService:Mode"] ?? "Mock", "Real", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IBeneficiaryServiceClient, MockBeneficiaryServiceClient>();
        }

        var merchantProviderMode = configuration["Providers:Merchant:Mode"] ?? "Mock";
        if (string.Equals(merchantProviderMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            var merchantConnectionString = configuration.GetConnectionString("MerchantDatabase")
                ?? throw new InvalidOperationException(
                    "Providers:Merchant:Mode is \"Sql\" but ConnectionStrings:MerchantDatabase is not configured.");
            services.AddScoped<IMerchantProvider>(_ => new SqlMerchantProvider(merchantConnectionString));
        }
        else
        {
            services.AddSingleton<IMerchantProvider, MockMerchantProvider>();
        }

        services.AddScoped<IMerchantService, MerchantService>();

        // "Real" mode for B2B is registered in Portal.Api's Program.cs — see the
        // Card/Beneficiary comment above for why.
        if (!string.Equals(configuration["Providers:B2BService:Mode"] ?? "Mock", "Real", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IB2BServiceClient, MockB2BServiceClient>();
        }

        services.AddScoped<IScreenService, ScreenService>();
        services.AddSingleton<ISampleScreenService, SampleScreenService>();

        services.AddScoped<IMockAdminService, MockAdminService>();
        services.AddScoped<IMockEngine, MockEngine>();

        return services;
    }
}
