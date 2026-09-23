using Application.Models;

namespace WeatherStation.Tests.Tests.Unit.ModelTest;

[TestFixture]
public sealed class JsonHomeValidationTest
{
    [Test]
    public void JsonHome_IsValid_WhenHomesEmpty_ReturnsFalse()
    {
        JsonHome jsonHome = new JsonHome
        {
            Body = new Body
            {
                Homes = new System.Collections.Generic.List<Home>()
            }
        };

        Assert.That(jsonHome.IsValid(), Is.False);
    }

    [Test]
    public void JsonHome_IsValid_WhenOneValidHome_ReturnsTrue()
    {
        JsonHome jsonHome = new JsonHome
        {
            Body = new Body
            {
                Homes = new System.Collections.Generic.List<Home>
                {
                    new Home { Id = "home-1", Name = "My home" }
                }
            }
        };

        Assert.That(jsonHome.IsValid(), Is.True);
    }

    [Test]
    public void Body_IsValid_WhenAnyHomeInvalid_ReturnsFalse()
    {
        Body body = new Body
        {
            Homes = new System.Collections.Generic.List<Home>
            {
                new Home { Id = "", Name = "My home" },
                new Home { Id = "home-2", Name = "My home" }
            }
        };

        Assert.That(body.IsValid(), Is.False);
    }

    [Test]
    public void Home_IsValid_WhenIdMissing_ReturnsFalse()
    {
        Home home = new Home { Id = " ", Name = "My home" };
        Assert.That(home.IsValid(), Is.False);
    }

    [Test]
    public void Home_IsValid_WhenNameMissing_ReturnsFalse()
    {
        Home home = new Home { Id = "home-1", Name = "" };
        Assert.That(home.IsValid(), Is.False);
    }

    [Test]
    public void Room_IsValid_WhenIdAndNamePresent_ReturnsTrue()
    {
        Room room = new Room
        {
            Id = "room-1",
            Name = "Living room",
            ModuleIds = new System.Collections.Generic.List<string> { "mod-1" }
        };

        Assert.That(room.IsValid(), Is.True);
    }

    [Test]
    public void Room_IsValid_WhenAnyModuleIdEmpty_ReturnsFalse()
    {
        Room room = new Room
        {
            Id = "room-1",
            Name = "Living room",
            ModuleIds = new System.Collections.Generic.List<string> { "mod-1", "" }
        };

        Assert.That(room.IsValid(), Is.False);
    }

    [Test]
    public void Module_IsValid_WhenMissingType_ReturnsFalse()
    {
        Module module = new Module
        {
            Id = "mod-1",
            Name = "Outdoor",
            Type = ""
        };

        Assert.That(module.IsValid(), Is.False);
    }

    [Test]
    public void User_IsValid_WhenMissingEmail_ReturnsFalse()
    {
        User user = new User
        {
            Id = "user-1",
            Email = ""
        };

        Assert.That(user.IsValid(), Is.False);
    }
}
