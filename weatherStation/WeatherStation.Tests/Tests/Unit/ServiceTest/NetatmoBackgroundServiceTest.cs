using API.Background.Services;
using API.Interface.Services;
using API.Auth.Netatmo.Interface;
using API.Interface.Helpers;
using API.Interface.Logic;
using API.Interface.Tasks;
using API.Auth.Netatmo.Options;
using API.Responses;
using Microsoft.Extensions.Options;

using Moq;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.ServiceTest;

[TestFixture]
public class NetatmoBackgroundServiceTest
{
    [Test]
    public async Task ExecuteTaskAsync_Authenticated_FetchAndStoreCalled()
    {
        Mock<INetatmoService> apiNetatmoService = new Mock<INetatmoService>();
        Mock<INetatmoTask> netatmoTask = new Mock<INetatmoTask>();
        Mock<INetatmoLogicDataProvider> netatmoLogicDataProvider = new Mock<INetatmoLogicDataProvider>();
        Mock<INetatmoTokenStore> netatmoTokenStore = new Mock<INetatmoTokenStore>();
        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions { Frequency = TimeSpan.FromMilliseconds(1) });
        Mock<INetatmoOAuthClient> netatmoOAuthClient = new Mock<INetatmoOAuthClient>();
        Mock<ITaskDelayer> taskDelayer = new Mock<ITaskDelayer>();
        Mock<ILogger> logger = new Mock<ILogger>();

        netatmoOAuthClient.Setup(x => x.Login(It.IsAny<CancellationToken>())).ReturnsAsync(new NetatmoAuthStatusResponse
        {
            Authenticated = true,
            Token = "token"
        });
        taskDelayer.Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        apiNetatmoService.Setup(x => 
            x.FetchAndStoreAsync(
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>())).
            ReturnsAsync(It.IsAny<int>());

        NetatmoService service = new NetatmoService(
            apiNetatmoService.Object,
            netatmoTask.Object,
            netatmoLogicDataProvider.Object,
            netatmoTokenStore.Object,
            options,
            netatmoOAuthClient.Object,
            taskDelayer.Object,
            logger.Object
        );

        using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(500, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        apiNetatmoService.Verify(x => x.FetchAndStoreAsync("token", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task ExecuteTaskAsync_NotAuthenticated_LogsErrorAndSkipsFetch()
    {
        Mock<INetatmoService> apiNetatmoService = new Mock<INetatmoService>();
        Mock<INetatmoTask> netatmoTask = new Mock<INetatmoTask>();
        Mock<INetatmoLogicDataProvider> netatmoLogicDataProvider = new Mock<INetatmoLogicDataProvider>();
        Mock<INetatmoTokenStore> netatmoTokenStore = new Mock<INetatmoTokenStore>();
        IOptions<NetatmoOptions> options = Options.Create(new NetatmoOptions { Frequency = TimeSpan.FromMilliseconds(1) });
        Mock<INetatmoOAuthClient> netatmoOAuthClient = new Mock<INetatmoOAuthClient>();
        Mock<ITaskDelayer> taskDelayer = new Mock<ITaskDelayer>();
        Mock<ILogger> logger = new Mock<ILogger>();

        netatmoOAuthClient.Setup(x => x.Login(It.IsAny<CancellationToken>())).ReturnsAsync(new NetatmoAuthStatusResponse
        {
            Authenticated = false,
            LoginUrl = "http://login.url"
        });
        taskDelayer.Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        NetatmoService service = new NetatmoService(
            apiNetatmoService.Object,
            netatmoTask.Object,
            netatmoLogicDataProvider.Object,
            netatmoTokenStore.Object,
            options,
            netatmoOAuthClient.Object,
            taskDelayer.Object,
            logger.Object
        );

        using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(500, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        logger.Verify(x => x.Error("You need to login at {url}", "http://login.url"), Times.AtLeastOnce);
        apiNetatmoService.Verify(x => x.FetchAndStoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
