using Microsoft.AspNetCore.Mvc;
using Portal.Api.Common;

namespace Portal.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected string CorrelationId => CorrelationIdAccessor.GetOrCreate(HttpContext);

    /// <summary>Wraps <paramref name="data"/> in the standard success envelope. Named
    /// distinctly from ControllerBase.Ok so it never shadows it for subclasses that
    /// need to return plain IActionResult (see AuthController).</summary>
    protected ActionResult<ApiResponse<T>> Envelope<T>(T data) =>
        Ok(ApiResponse<T>.Ok(data, CorrelationId));
}
