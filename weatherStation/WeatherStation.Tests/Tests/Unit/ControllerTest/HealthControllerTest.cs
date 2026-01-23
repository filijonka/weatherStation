using API.Controllers.v1;

using Microsoft.AspNetCore.Mvc;

namespace WeatherStation.Tests.Tests.Unit.ControllerTest;

/// <summary>
/// Tests for health controller.
/// </summary>
[TestFixture]
public class HealthControllerTest
{
    /// <summary>
    /// Get health should return healthy status.
    /// </summary>
    [Test]
    public void Test_GetHealth_ReturnsHealthy()
    {
        HealthController controller = new HealthController();

        ActionResult<HealthController.HealthResponse> result = controller.GetHealth();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        HealthController.HealthResponse response = okResult.Value as HealthController.HealthResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Status, Is.EqualTo("Healthy"));
    }
}
