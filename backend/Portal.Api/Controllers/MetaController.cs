using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Api.Common;
using Portal.Application.Common;

namespace Portal.Api.Controllers;

/// <summary>
/// Unauthenticated, safe metadata for the frontend (environment banner on the login page).
/// Never returns connection strings, secrets, or internal hostnames. See docs/12-environments.md.
/// </summary>
[AllowAnonymous]
public sealed class MetaController(IEnvironmentContext environmentContext) : ApiControllerBase
{
    [HttpGet("environment")]
    public ActionResult<ApiResponse<EnvironmentInfo>> GetEnvironment() =>
        Envelope(environmentContext.ToPublicInfo());
}
