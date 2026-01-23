using API.Extensions;
using API.Helpers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace WeatherStation.Tests.Tests.Unit.HelperTest;

/// <summary>
/// Tests for API key auth operation filter.
/// </summary>
[TestFixture]
public class ApiKeyAuthOperationFilterTest : TestBase
{
    /// <summary>
    /// Apply should add security requirement when method has ApiKeyAuth attribute.
    /// </summary>
    [Test]
    public void Test_Apply_WithApiKeyAuthAttribute_AddsSecurityRequirement()
    {
        ApiKeyAuthOperationFilter filter = new ApiKeyAuthOperationFilter();
        OpenApiOperation operation = new OpenApiOperation();
        MethodInfo methodInfo = typeof(TestController).GetMethod(nameof(TestController.MethodWithAttribute), BindingFlags.Public | BindingFlags.Instance);
        OperationFilterContext context = CreateOperationFilterContext(methodInfo);

        filter.Apply(operation, context);

        Assert.That(operation.Security, Is.Not.Null);
        Assert.That(operation.Security.Count, Is.GreaterThan(0));
    }

    /// <summary>
    /// Apply should add security requirement when controller has ApiKeyAuth attribute.
    /// </summary>
    [Test]
    public void Test_Apply_WithApiKeyAuthAttributeOnController_AddsSecurityRequirement()
    {
        ApiKeyAuthOperationFilter filter = new ApiKeyAuthOperationFilter();
        OpenApiOperation operation = new OpenApiOperation();
        MethodInfo methodInfo = typeof(TestControllerWithAttribute).GetMethod(nameof(TestControllerWithAttribute.Method), BindingFlags.Public | BindingFlags.Instance);
        OperationFilterContext context = CreateOperationFilterContext(methodInfo);

        filter.Apply(operation, context);

        Assert.That(operation.Security, Is.Not.Null);
        Assert.That(operation.Security.Count, Is.GreaterThan(0));
    }

    /// <summary>
    /// Apply should not add security requirement when no attribute.
    /// </summary>
    [Test]
    public void Test_Apply_WithoutAttribute_NoSecurityRequirement()
    {
        ApiKeyAuthOperationFilter filter = new ApiKeyAuthOperationFilter();
        OpenApiOperation operation = new OpenApiOperation();
        MethodInfo methodInfo = typeof(TestController).GetMethod(nameof(TestController.MethodWithoutAttribute), BindingFlags.Public | BindingFlags.Instance);
        OperationFilterContext context = CreateOperationFilterContext(methodInfo);

        filter.Apply(operation, context);

        Assert.That(operation.Security, Is.Empty);
    }

    private class TestController : ControllerBase
    {
        [ApiKeyAuth]
        public void MethodWithAttribute() { }

        public void MethodWithoutAttribute() { }
    }

    [ApiKeyAuth]
    private class TestControllerWithAttribute : ControllerBase
    {
        public void Method() { }
    }
}
