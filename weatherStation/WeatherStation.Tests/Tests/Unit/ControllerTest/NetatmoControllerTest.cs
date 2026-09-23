using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;
using API.Controllers.v1;
using API.Exceptions;
using API.Filters;
using API.Interface.Logic;
using Application.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.ControllerTest;

/// <summary>
/// Tests for Netatmo data controller.
/// </summary>
[TestFixture]
public class NetatmoControllerTest
{
    private const string StationDeviceId = "AA:BB:CC:00:00:10";

    private static NetatmoController CreateController(
        INetatmoTokenStore tokenStore,
        INetatmoLogicDataProvider logicDataProvider,
        ILogger logger,
        string accessToken = "test-token")
    {
        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "https",
                Host = new HostString("localhost:8080")
            }
        };

        if (accessToken != null)
        {
            httpContext.Items[NetatmoAuthenticatedFilter.HttpContextItemAccessTokenKey] = accessToken;
        }

        NetatmoController controller = new NetatmoController(
            tokenStore,
            logicDataProvider,
            logger
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static async Task<JsonStationData> LoadStationDataFixtureAsync()
    {
        string fixturePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "response_stationdata.json");
        string responseBody = await File.ReadAllTextAsync(fixturePath, CancellationToken.None);

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        JsonStationData stationData = JsonConvert.DeserializeObject<JsonStationData>(responseBody, settings);
        Assert.That(stationData, Is.Not.Null);
        return stationData!;
    }

    [Test]
    public async Task Test_GetHomesDataAsync_WhenNotAuthenticated_Returns401WithApiResponse()
    {
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo)null);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object, null);

        ActionResult<ApiResponse<JsonHome>> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ApiResponse<JsonHome>>());

        ApiResponse<JsonHome> response = objectResult.Value as ApiResponse<JsonHome>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Data, Is.Not.Null);
        });

        logicDataProviderMock.Verify(
            d => d.GetHomesDataAsync(null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Test_GetHomesDataAsync_WhenAuthenticatedButNoData_Returns502()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonHome());

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        ActionResult<ApiResponse<JsonHome>> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ApiResponse<JsonHome>>());

        ApiResponse<JsonHome> response = objectResult.Value as ApiResponse<JsonHome>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Error, Is.EqualTo("No home data was returned. Please try again later."));
            Assert.That(response.Data, Is.Not.Null);
        });
    }

    [Test]
    public async Task Test_GetHomesDataAsync_WhenAuthenticated_ReturnsJsonData()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        JsonHome jsonData = new JsonHome
        {
            Body = new Body
            {
                Homes = new System.Collections.Generic.List<Home> { new Home { Id = "home-1", Name = "Home" } }
            }
        };

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonData);

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        ActionResult<ApiResponse<JsonHome>> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.InstanceOf<ApiResponse<JsonHome>>());

        ApiResponse<JsonHome> response = okResult.Value as ApiResponse<JsonHome>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.True);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Data, Is.SameAs(jsonData));
            Assert.That(response.Error, Is.EqualTo(string.Empty));
        });

        logicDataProviderMock.Verify(
            d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Test_GetHomesDataAsync_WithGatewayTypes_ParsesAndPassesFilter()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        JsonHome jsonData = new JsonHome
        {
            Body = new Body
            {
                Homes = new System.Collections.Generic.List<Home> { new Home { Id = "home-1", Name = "Home" } }
            }
        };

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetHomesDataAsync(
                "test-token",
                It.Is<string[]>(arr => arr != null && arr.Length == 2 && arr[0] == "NLG" && arr[1] == "OTH"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonData);

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        _ = await controller.GetHomesDataAsync("NLG, OTH", CancellationToken.None);

        logicDataProviderMock.Verify(
            d => d.GetHomesDataAsync(
                "test-token",
                It.Is<string[]>(arr => arr != null && arr.Length == 2 && arr[0] == "NLG" && arr[1] == "OTH"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Test_GetHomesDataAsync_WithEmptyGatewayTypes_PassesNull()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        JsonHome jsonData = new JsonHome
        {
            Body = new Body
            {
                Homes = new System.Collections.Generic.List<Home> { new Home { Id = "home-1", Name = "Home" } }
            }
        };

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonData);

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        _ = await controller.GetHomesDataAsync("", CancellationToken.None);

        logicDataProviderMock.Verify(
            d => d.GetHomesDataAsync("test-token", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Test_GetHomesDataAsync_WhenDataProviderThrows_Returns502()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetHomesDataAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API error"));

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        ActionResult<ApiResponse<JsonHome>> result = await controller.GetHomesDataAsync(null, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ApiResponse<JsonHome>>());

        ApiResponse<JsonHome> response = objectResult.Value as ApiResponse<JsonHome>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Error, Is.EqualTo("Failed to retrieve home data from Netatmo. Please try again later."));
            Assert.That(response.Data, Is.Not.Null);
        });
    }

    [Test]
    public async Task Test_GetModuleDataAsync_WhenNotAuthenticated_Returns401WithApiResponse()
    {
        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetatmoTokenInfo)null);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();

        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object, null);

        ActionResult<ApiResponse<JsonStationData>> result = await controller.GetModuleDataAsync(StationDeviceId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ApiResponse<JsonStationData>>());

        ApiResponse<JsonStationData> response = objectResult.Value as ApiResponse<JsonStationData>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Data, Is.Not.Null);
        });

        logicDataProviderMock.Verify(
            d => d.GetModuleDataAsync(null, StationDeviceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Test_GetModuleDataAsync_WhenModuleIdInvalid_Returns400()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        ActionResult<ApiResponse<JsonStationData>> result = await controller.GetModuleDataAsync("not-a-mac", CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        BadRequestObjectResult badRequest = result.Result as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.Value, Is.InstanceOf<ApiResponse<JsonStationData>>());

        ApiResponse<JsonStationData> response = badRequest.Value as ApiResponse<JsonStationData>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Error, Is.EqualTo("Invalid module id format."));
        });

        logicDataProviderMock.Verify(
            d => d.GetModuleDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Test_GetModuleDataAsync_WhenNetatmoReturns400_Returns400()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetModuleDataAsync("test-token", StationDeviceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NetatmoBadRequestException(3, "Invalid argument"));

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        ActionResult<ApiResponse<JsonStationData>> result = await controller.GetModuleDataAsync(StationDeviceId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        BadRequestObjectResult badRequest = result.Result as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.Value, Is.InstanceOf<ApiResponse<JsonStationData>>());

        ApiResponse<JsonStationData> response = badRequest.Value as ApiResponse<JsonStationData>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Error, Is.EqualTo("Invalid argument"));
        });
    }

    [Test]
    public async Task Test_GetModuleDataAsync_WhenAuthenticatedButNoData_Returns502()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetModuleDataAsync("test-token", StationDeviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonStationData());

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        ActionResult<ApiResponse<JsonStationData>> result = await controller.GetModuleDataAsync(StationDeviceId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ApiResponse<JsonStationData>>());

        ApiResponse<JsonStationData> response = objectResult.Value as ApiResponse<JsonStationData>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Error, Is.EqualTo("No station data was returned. Please try again later."));
        });
    }

    [Test]
    public async Task Test_GetModuleDataAsync_WhenAuthenticated_ReturnsJsonDataFromFixture()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        JsonStationData stationData = await LoadStationDataFixtureAsync();
        Assert.That(stationData.IsValid(), Is.True);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetModuleDataAsync("test-token", StationDeviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stationData);

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        ActionResult<ApiResponse<JsonStationData>> result = await controller.GetModuleDataAsync(StationDeviceId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.InstanceOf<ApiResponse<JsonStationData>>());

        ApiResponse<JsonStationData> response = okResult.Value as ApiResponse<JsonStationData>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.True);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Data, Is.SameAs(stationData));
            Assert.That(response.Error, Is.EqualTo(string.Empty));
        });

        logicDataProviderMock.Verify(
            d => d.GetModuleDataAsync("test-token", StationDeviceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Test_GetModuleDataAsync_WhenProviderThrows_Returns502()
    {
        NetatmoTokenInfo tokenInfo = new NetatmoTokenInfo
        {
            AccessToken = "test-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Mock<INetatmoTokenStore> tokenStoreMock = new Mock<INetatmoTokenStore>();
        tokenStoreMock
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenInfo);

        Mock<INetatmoLogicDataProvider> logicDataProviderMock = new Mock<INetatmoLogicDataProvider>();
        logicDataProviderMock
            .Setup(d => d.GetModuleDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        Mock<ILogger> loggerMock = new Mock<ILogger>();
        NetatmoController controller = CreateController(tokenStoreMock.Object, logicDataProviderMock.Object, loggerMock.Object);

        ActionResult<ApiResponse<JsonStationData>> result = await controller.GetModuleDataAsync(StationDeviceId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        ObjectResult objectResult = result.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(502));
        Assert.That(objectResult.Value, Is.InstanceOf<ApiResponse<JsonStationData>>());

        ApiResponse<JsonStationData> response = objectResult.Value as ApiResponse<JsonStationData>;
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsSuccess, Is.False);
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.Error, Is.EqualTo("Failed to retrieve station data from Netatmo. Please try again later."));
        });
    }
}
