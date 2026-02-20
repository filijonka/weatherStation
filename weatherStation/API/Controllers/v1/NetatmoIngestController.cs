using API.Interface;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using API.Filters;

namespace API.Controllers.v1;

/// <summary>
/// Ingestion endpoints for external weather data.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ingest")]
public sealed class NetatmoIngestController : ControllerBase
{
    private readonly INetatmoIngestService ingestService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoIngestController"/> class.
    /// </summary>
    /// <param name="ingestService">Ingestion service.</param>
    public NetatmoIngestController(INetatmoIngestService ingestService)
    {
        this.ingestService = ingestService;
    }

    /// <summary>
    /// Triggers ingestion and returns the number of readings written.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Ingestion result payload.</returns>
    [HttpPost]
    [NetatmoAuthenticated]
    public async Task<ActionResult<IngestResponse>> IngestAsync(CancellationToken cancellationToken)
    {
        string accessToken = (string)HttpContext.Items[NetatmoAuthenticatedFilter.HttpContextItemAccessTokenKey]!;
        int numberOfPoints = await this.ingestService.FetchAndStoreAsync(accessToken, cancellationToken);
        IngestResponse response = new IngestResponse(numberOfPoints);
        return this.Ok(response);
    }

    /// <summary>
    /// Ingestion response payload.
    /// </summary>
    /// <param name="WrittenCount">Number of readings written.</param>
    public sealed record IngestResponse(int WrittenCount);
}
