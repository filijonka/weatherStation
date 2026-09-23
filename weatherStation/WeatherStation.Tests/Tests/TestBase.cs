using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WeatherStation.Tests.Tests;

/// <summary>
/// Base class for all tests with common setup.
/// </summary>
public class TestBase
{
    /// <summary>
    /// Test configuration.
    /// </summary>
    protected IConfigurationRoot TestConfiguration;

    /// <summary>
    /// Sets up base test configuration.
    /// </summary>
    [SetUp]
    public void SetUpBase()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());

        List<KeyValuePair<string, string>> appSettings = new List<KeyValuePair<string, string>>();

        this.TestConfiguration = this.TestConfiguration is null
            ? new ConfigurationBuilder()
                .AddInMemoryCollection(appSettings)
                .Build()
            : this.TestConfiguration;
    }

    /// <summary>
    /// Creates an OperationFilterContext instance for testing Swagger operation filters.
    /// </summary>
    /// <param name="methodInfo">The method info to use in the context.</param>
    /// <returns>An OperationFilterContext instance with the specified method info.</returns>
    protected static OperationFilterContext CreateOperationFilterContext(MethodInfo methodInfo)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        ApiDescription apiDescription = new ApiDescription();
        ISchemaGenerator schemaGenerator = new Mock<ISchemaGenerator>().Object;
        SchemaRepository schemaRepository = new SchemaRepository();

        return new OperationFilterContext(
            apiDescription,
            schemaGenerator,
            schemaRepository,
            methodInfo
        );
    }
}
