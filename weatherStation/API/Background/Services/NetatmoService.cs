using ApiINetatmoService = API.Interface.Services.INetatmoService;
using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;
using API.Helpers;
using API.Interface.Helpers;
using API.Interface.Logic;
using API.Interface.Tasks;
using API.Responses;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace API.Background.Services;

/// <summary>
/// The background service for fetching data from netatmo
/// </summary>
public class NetatmoService : BackgroundService
{
    private readonly ApiINetatmoService apiNetatmoService;
    private readonly INetatmoTask netatmoTask;
    private readonly INetatmoLogicDataProvider netatmoLogicDataProvider;
    private readonly INetatmoTokenStore netatmoTokenStore;
    private readonly IOptions<NetatmoOptions> options;
    private readonly ITaskDelayer taskDelayer;
    private readonly ILogger logger;
    private readonly INetatmoOAuthClient netatmoOAuthClient;

    /// <summary>
    /// Constructor for the service
    /// </summary>
    /// <param name="apiNetatmoService"></param>
    /// <param name="netatmoTask"></param>
    /// <param name="netatmoLogicDataProvider"></param>
    /// <param name="netatmoTokenStore"></param>
    /// <param name="options"></param>
    /// <param name="netatmoOAuthClient"></param>
    /// <param name="taskDelayer"></param>
    /// <param name="logger"></param>
    public NetatmoService(
        ApiINetatmoService apiNetatmoService,
        INetatmoTask netatmoTask,
        INetatmoLogicDataProvider netatmoLogicDataProvider,
        INetatmoTokenStore netatmoTokenStore,
        IOptions<NetatmoOptions> options,
        INetatmoOAuthClient netatmoOAuthClient,
        ITaskDelayer taskDelayer,
        ILogger logger
    )
    {
        this.apiNetatmoService = apiNetatmoService;
        this.netatmoTask = netatmoTask;
        this.netatmoLogicDataProvider = netatmoLogicDataProvider;
        this.netatmoTokenStore = netatmoTokenStore;
        this.options = options;
        this.netatmoOAuthClient = netatmoOAuthClient;
        this.taskDelayer = taskDelayer;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await taskDelayer.Delay(options.Value.Frequency, stoppingToken);
            //check that we are authenticated at netatmo
            NetatmoAuthStatusResponse authStatus = await this.netatmoOAuthClient.Login(stoppingToken);
            if (!authStatus.Authenticated)
            {
                this.logger.Error("You need to login at {url}",authStatus.LoginUrl);
                continue;
            }
            await this.apiNetatmoService.FetchAndStoreAsync(authStatus.Token, stoppingToken);
        }
    }
    
    // This is a wrapper to allow direct testing of the protected ExecuteAsync method
    internal virtual async Task ExecuteTaskAsync(CancellationToken stoppingToken) => await ExecuteAsync(stoppingToken);
}