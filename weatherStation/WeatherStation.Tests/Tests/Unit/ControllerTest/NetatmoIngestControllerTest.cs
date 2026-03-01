using API.Controllers.v1;
using API.Filters;
using API.Interface;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Threading;
using API.Interface.Services;
using WeatherStation.Tests.Tests;
using It = Moq.It;

namespace WeatherStation.Tests.Tests.Unit.ControllerTest;

/// <summary>
/// Tests for ingest controller.
/// </summary>
[TestFixture]
public class NetatmoIngestControllerTest
{
    [Test]
    public async Task Test_Ingest_ReturnsWrittenCount()
    {
        Mock<INetatmoService> ingestService = new ServiceTestMockBuilder<INetatmoService>.Builder()
            .Setup(s => s.FetchAndStoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Task.FromResult(5))
            .Build();

        NetatmoIngestController controller = new NetatmoIngestController(ingestService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Items[NetatmoAuthenticatedFilter.HttpContextItemAccessTokenKey] = "test-token";

        ActionResult<NetatmoIngestController.IngestResponse> result = await controller.IngestAsync(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        NetatmoIngestController.IngestResponse response = okResult.Value as NetatmoIngestController.IngestResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.WrittenCount, Is.EqualTo(5));
    }

    [Test]
    public void Test_Ingest_ServiceThrowsException()
    {
        Mock<INetatmoService> ingestService = new ServiceTestMockBuilder<INetatmoService>.Builder()
            .SetupException(s => s.FetchAndStoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), new InvalidOperationException("Service error"))
            .Build();

        NetatmoIngestController controller = new NetatmoIngestController(ingestService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Items[NetatmoAuthenticatedFilter.HttpContextItemAccessTokenKey] = "test-token";

        Assert.ThrowsAsync<InvalidOperationException>(async () => await controller.IngestAsync(CancellationToken.None));
    }
}
