using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.v1;

/// <summary>
/// Health check endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Returns service health status.
    /// </summary>
    /// <returns>Health response payload.</returns>
    [HttpGet]
    public ActionResult<HealthResponse> GetHealth()
    {
        HealthResponse response = new HealthResponse("Healthy");
        return this.Ok(response);
    }

    /// <summary>
    /// Health response payload.
    /// </summary>
    /// <param name="Status">Health status string.</param>
    public sealed record HealthResponse(string Status);
}
