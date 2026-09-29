using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Portal.Api.Authorization;
using Portal.Api.Common;
using Portal.Api.Middleware;
using Portal.Application.Auth;
using Portal.Infrastructure;
using Portal.Infrastructure.Identity;
using Portal.Infrastructure.Mocking;
using Portal.Infrastructure.Persistence;
using Portal.Infrastructure.Screens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services
        .AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            // Replace the default ProblemDetails validation response with the
            // portal's standard envelope (docs/06-api-design.md §2-3).
            options.InvalidModelStateResponseFactory = context =>
            {
                var correlationId = CorrelationIdAccessor.GetOrCreate(context.HttpContext);
                var details = context.ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .Select(kvp => new
                    {
                        field = kvp.Key,
                        message = string.Join(" ", kvp.Value!.Errors.Select(e => e.ErrorMessage))
                    });

                var response = ApiResponse<object>.Fail(
                    new ApiError { Code = "VALIDATION_FAILED", Message = "One or more fields are invalid.", Details = details },
                    correlationId);

                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
            };
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Test Data Management Portal API",
            Version = "v1",
            Description = "Internal QA/Development portal API. See /docs in the repository for full design documentation."
        });

        var jwtScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the access token returned by /api/v1/auth/login.",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        options.AddSecurityDefinition("Bearer", jwtScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = [] });
    });

    builder.Services.AddHealthChecks();

    builder.Services.AddInfrastructure(builder.Configuration);

    // "Real" mode wiring lives here, not in Portal.Infrastructure — only Api may
    // reference Portal.Integrations (docs/01-architecture.md's dependency rule).
    if (string.Equals(builder.Configuration["Providers:CardManagement:Mode"], "Real", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddHttpClient<Portal.Application.Customers.ICardManagementClient, Portal.Integrations.Customers.HttpCardManagementClient>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Integrations:CardManagement:BaseUrl"]
                ?? throw new InvalidOperationException("Integrations:CardManagement:BaseUrl is required when Providers:CardManagement:Mode is \"Real\"."));
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }

    if (string.Equals(builder.Configuration["Providers:BeneficiaryService:Mode"], "Real", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddHttpClient<Portal.Application.Customers.IBeneficiaryServiceClient, Portal.Integrations.Customers.HttpBeneficiaryServiceClient>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Integrations:BeneficiaryService:BaseUrl"]
                ?? throw new InvalidOperationException("Integrations:BeneficiaryService:BaseUrl is required when Providers:BeneficiaryService:Mode is \"Real\"."));
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }

    if (string.Equals(builder.Configuration["Providers:B2BService:Mode"], "Real", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddHttpClient<Portal.Application.Merchants.IB2BServiceClient, Portal.Integrations.Merchants.HttpB2BServiceClient>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Integrations:B2BService:BaseUrl"]
                ?? throw new InvalidOperationException("Integrations:B2BService:BaseUrl is required when Providers:B2BService:Mode is \"Real\"."));
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }

    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

    // Resolve the JWT signing key once so token issuance (AuthService, via
    // IOptions<JwtOptions>) and token validation (JwtBearerOptions below) are
    // guaranteed to use identical bytes. See docs/03-security.md §2.
    var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
    if (string.IsNullOrWhiteSpace(jwtSigningKey))
    {
        if (builder.Environment.IsDevelopment())
        {
            jwtSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            Log.Warning(
                "Jwt:SigningKey is not configured — generated an ephemeral development-only signing key. " +
                "Tokens will be invalidated on restart. Set the JWT__SigningKey environment variable for a stable local key.");
        }
        else
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be supplied via environment variable outside Development. See docs/12-environments.md.");
        }
    }
    builder.Services.PostConfigure<JwtOptions>(o => o.SigningKey = jwtSigningKey);

    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MockerPortal";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MockerPortal.Clients";

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
    builder.Services.AddAuthorization();

    // docs/03-security.md §2 rule 7: slow credential stuffing/brute force against
    // the unauthenticated auth endpoints. Partitioned per client IP so one abusive
    // caller can't exhaust the limit for everyone else.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5173"];

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // refresh token travels as an httpOnly cookie — see docs/04 §1.
        });
    });

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<Portal.Api.Middleware.SecurityHeadersMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/api/v1/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.Write
    });

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
        {
            var dbContext = services.GetRequiredService<PortalDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var seedDbContext = services.GetRequiredService<PortalDbContext>();
        await IdentitySeeder.SeedRolesAndPermissionsAsync(roleManager, seedDbContext, logger);

        if (app.Environment.IsDevelopment())
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            await IdentitySeeder.SeedDevelopmentUsersAsync(userManager, logger);
        }

        await ScreenSeeder.SeedAllAsync(seedDbContext);
        await MockDataSeeder.SeedAllAsync(seedDbContext);
    }

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory-based API tests.
public partial class Program { }
