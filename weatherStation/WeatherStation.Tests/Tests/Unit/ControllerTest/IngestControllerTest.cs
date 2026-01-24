using API.Controllers.v1;
using API.Interface;

using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Threading;
using WeatherStation.Tests.Tests;
using It = Moq.It;

namespace WeatherStation.Tests.Tests.Unit.ControllerTest;

/// <summary>
/// Tests for ingest controller.
/// </summary>
[TestFixture]
public class IngestControllerTest
{
    [Test]
    public async Task Test_Ingest_ReturnsWrittenCount()
    {
        Mock<IWeatherIngestService> ingestService = new ServiceTestMockBuilder<IWeatherIngestService>.Builder()
            .Setup(s => s.FetchAndStoreAsync(It.IsAny<CancellationToken>()), Task.FromResult(5))
            .Build();

        IngestController controller = new IngestController(ingestService.Object);

        ActionResult<IngestController.IngestResponse> result = await controller.IngestAsync(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        IngestController.IngestResponse response = okResult.Value as IngestController.IngestResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.WrittenCount, Is.EqualTo(5));
    }

    [Test]
    public void Test_Ingest_ServiceThrowsException()
    {
        Mock<IWeatherIngestService> ingestService = new ServiceTestMockBuilder<IWeatherIngestService>.Builder()
            .SetupException(s => s.FetchAndStoreAsync(It.IsAny<CancellationToken>()), new InvalidOperationException("Service error"))
            .Build();

        IngestController controller = new IngestController(ingestService.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await controller.IngestAsync(CancellationToken.None));
    }
}
