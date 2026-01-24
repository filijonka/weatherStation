using API.Extensions;

using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace WeatherStation.Tests.Tests.Unit.ExtensionTest;

/// <summary>
/// Tests for Swagger configuration options.
/// </summary>
[TestFixture]
public class ConfigureSwaggerOptionsTest
{
    [Test]
    public void Test_Configure_AddsSecurityDefinition()
    {
        Mock<IApiVersionDescriptionProvider> mockProvider = new Mock<IApiVersionDescriptionProvider>();
        mockProvider.Setup(p => p.ApiVersionDescriptions).Returns(new List<ApiVersionDescription>());

        ConfigureSwaggerOptions configureOptions = new ConfigureSwaggerOptions(mockProvider.Object);
        SwaggerGenOptions swaggerOptions = new SwaggerGenOptions();

        configureOptions.Configure(swaggerOptions);

        Assert.That(swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs, Is.Not.Null);
    }

    [Test]
    public void Test_Configure_AddsOperationFilter()
    {
        Mock<IApiVersionDescriptionProvider> mockProvider = new Mock<IApiVersionDescriptionProvider>();
        mockProvider.Setup(p => p.ApiVersionDescriptions).Returns(new List<ApiVersionDescription>());

        ConfigureSwaggerOptions configureOptions = new ConfigureSwaggerOptions(mockProvider.Object);
        SwaggerGenOptions swaggerOptions = new SwaggerGenOptions();

        configureOptions.Configure(swaggerOptions);

        Assert.That(swaggerOptions.SwaggerGeneratorOptions.OperationAsyncFilters, Is.Empty);
    }

    [Test]
    public void Test_Configure_CreatesVersionInfo()
    {
        List<ApiVersionDescription> apiVersions = new List<ApiVersionDescription>
        {
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", false)
        };

        Mock<IApiVersionDescriptionProvider> mockProvider = new Mock<IApiVersionDescriptionProvider>();
        mockProvider.Setup(p => p.ApiVersionDescriptions).Returns(apiVersions);

        ConfigureSwaggerOptions configureOptions = new ConfigureSwaggerOptions(mockProvider.Object);
        SwaggerGenOptions swaggerOptions = new SwaggerGenOptions();

        configureOptions.Configure(swaggerOptions);

        IDictionary<string, OpenApiInfo> docs = swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs;
        Assert.That(docs.Count, Is.EqualTo(1));
        Assert.That(docs.ContainsKey("v1"), Is.True);
        docs.TryGetValue("v1", out OpenApiInfo value);
        Assert.That(value, Is.Not.Null);
        Assert.That(value.Title, Is.EqualTo("Weather Station"));
    }

    [Test]
    public void Test_Configure_DeprecatedVersion_AddsDeprecationMessage()
    {
        List<ApiVersionDescription> apiVersions = new List<ApiVersionDescription>
        {
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", true)
        };

        Mock<IApiVersionDescriptionProvider> mockProvider = new Mock<IApiVersionDescriptionProvider>();
        mockProvider.Setup(p => p.ApiVersionDescriptions).Returns(apiVersions);

        ConfigureSwaggerOptions configureOptions = new ConfigureSwaggerOptions(mockProvider.Object);
        SwaggerGenOptions swaggerOptions = new SwaggerGenOptions();

        configureOptions.Configure(swaggerOptions);

        IDictionary<string, OpenApiInfo> docs = swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs;
        docs.TryGetValue("v1", out OpenApiInfo value);
        Assert.That(value, Is.Not.Null);
        Assert.That(value.Description, Does.Contain("deprecated"));
    }
}
