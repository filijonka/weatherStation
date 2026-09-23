using System;
using API.Auth.Netatmo.Options;

namespace WeatherStation.Tests.Tests.Unit.OptionsTest;

/// <summary>
/// Tests for NetatmoOptions validation.
/// </summary>
[TestFixture]
public class NetatmoOptionsTest
{
    [Test]
    public void Test_Validate_WhenClientIdEmpty_Throws()
    {
        NetatmoOptions options = new NetatmoOptions
        {
            ClientId = "",
            ClientSecret = "s",
            RedirectUri = "http://x",
            Scopes = "read_station",
            AuthorizeUrl = "https://auth",
            TokenUrl = "https://token",
            TokenFilePath = "t.json"
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain("ClientId"));
    }

    [Test]
    public void Test_Validate_WhenAllRequiredSet_DoesNotThrow()
    {
        NetatmoOptions options = new NetatmoOptions
        {
            ClientId = "id",
            ClientSecret = "secret",
            RedirectUri = "http://localhost/cb",
            Scopes = "read_station",
            AuthorizeUrl = "https://api.netatmo.com/oauth2/authorize",
            TokenUrl = "https://api.netatmo.com/oauth2/token",
            TokenFilePath = "tokens.json"
        };

        Assert.DoesNotThrow(() => options.Validate());
    }
}
