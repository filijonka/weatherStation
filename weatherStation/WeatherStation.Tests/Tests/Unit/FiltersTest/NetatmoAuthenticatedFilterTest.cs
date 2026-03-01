using API.Filters;
using API.Auth.Netatmo.Interface;
using API.Responses;
using Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStation.Tests.Tests.Unit.FiltersTest;

[TestFixture]
public class NetatmoAuthenticatedFilterTest
{
    [Test]
    public async Task OnActionExecutionAsync_Authenticated_SetsAccessTokenAndCallsNext()
    {
        Mock<INetatmoTokenStore> tokenStore = new Mock<INetatmoTokenStore>();
        Mock<INetatmoOAuthClient> oauthClient = new Mock<INetatmoOAuthClient>();
        NetatmoAuthStatusResponse authStatus = new NetatmoAuthStatusResponse
        {
            Authenticated = true,
            Token = "token123"
        };
        oauthClient.Setup(x => x.Login(It.IsAny<CancellationToken>())).ReturnsAsync(authStatus);

        DefaultHttpContext httpContext = new DefaultHttpContext();
        ActionExecutingContext context = new ActionExecutingContext(
            new ActionContext {
                HttpContext = httpContext,
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            },
            new System.Collections.Generic.List<IFilterMetadata>(),
            new System.Collections.Generic.Dictionary<string, object>(),
            null
        );
        bool nextCalled = false;
        ActionExecutionDelegate next = () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null); };

        NetatmoAuthenticatedFilter filter = new NetatmoAuthenticatedFilter(tokenStore.Object, oauthClient.Object);
        await filter.OnActionExecutionAsync(context, next);

        Assert.That(httpContext.Items[NetatmoAuthenticatedFilter.HttpContextItemAccessTokenKey], Is.EqualTo("token123"));
        Assert.That(nextCalled, Is.True);
        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task OnActionExecutionAsync_NotAuthenticated_SetsUnauthorizedResult()
    {
        Mock<INetatmoTokenStore> tokenStore = new Mock<INetatmoTokenStore>();
        Mock<INetatmoOAuthClient> oauthClient = new Mock<INetatmoOAuthClient>();
        NetatmoAuthStatusResponse authStatus = new NetatmoAuthStatusResponse
        {
            Authenticated = false,
            LoginUrl = "http://login.url"
        };
        oauthClient.Setup(x => x.Login(It.IsAny<CancellationToken>())).ReturnsAsync(authStatus);

        DefaultHttpContext httpContext = new DefaultHttpContext();
        ActionExecutingContext context = new ActionExecutingContext(
            new ActionContext {
                HttpContext = httpContext,
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            },
            new System.Collections.Generic.List<IFilterMetadata>(),
            new System.Collections.Generic.Dictionary<string, object>(),
            null
        );
        bool nextCalled = false;
        ActionExecutionDelegate next = () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null); };

        NetatmoAuthenticatedFilter filter = new NetatmoAuthenticatedFilter(tokenStore.Object, oauthClient.Object);
        await filter.OnActionExecutionAsync(context, next);

        Assert.That(context.Result, Is.InstanceOf<UnauthorizedObjectResult>());
        Assert.That(nextCalled, Is.False);
    }
}
