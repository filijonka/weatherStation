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
    [Test]
    public void Test_Apply_WithApiKeyAuthAttribute_AddsSecurityRequirement()
    {
        ApiKeyAuthOperationFilter filter = new ApiKeyAuthOperationFilter();
        OpenApiOperation operation = new OpenApiOperation();
        MethodInfo methodInfo = typeof(TestController).GetMethod(
            nameof(TestController.MethodWithAttribute),
            BindingFlags.Public | BindingFlags.Static
        );
        OperationFilterContext context = CreateOperationFilterContext(methodInfo);

        filter.Apply(operation, context);

        Assert.That(operation.Security, Is.Not.Null);
        Assert.That(operation.Security, Is.Not.Empty);
    }

    [Test]
    public void Test_Apply_WithApiKeyAuthAttributeOnController_AddsSecurityRequirement()
    {
        ApiKeyAuthOperationFilter filter = new ApiKeyAuthOperationFilter();
        OpenApiOperation operation = new OpenApiOperation();
        MethodInfo methodInfo = typeof(TestControllerWithAttribute).GetMethod(
            nameof(TestControllerWithAttribute.Method),
            BindingFlags.Public | BindingFlags.Static
        );
        OperationFilterContext context = CreateOperationFilterContext(methodInfo);

        filter.Apply(operation, context);

        Assert.That(operation.Security, Is.Not.Null);
        Assert.That(operation.Security, Is.Not.Empty);
    }

    [Test]
    public void Test_Apply_WithoutAttribute_NoSecurityRequirement()
    {
        ApiKeyAuthOperationFilter filter = new ApiKeyAuthOperationFilter();
        OpenApiOperation operation = new OpenApiOperation();
        MethodInfo methodInfo = typeof(TestController).GetMethod(
            nameof(TestController.MethodWithoutAttribute),
            BindingFlags.Public | BindingFlags.Static
        );
        OperationFilterContext context = CreateOperationFilterContext(methodInfo);

        filter.Apply(operation, context);

        Assert.That(operation.Security, Is.Empty);
    }

    private class TestController : ControllerBase
    {
        [ApiKeyAuth]
        public static void MethodWithAttribute()
        {
            // Method intentionally left empty.
        }

        public static void MethodWithoutAttribute()
        {
            // Method intentionally left empty.
        }
    }

    [ApiKeyAuth]
    private class TestControllerWithAttribute : ControllerBase
    {
        public static void Method()
        {
            // Method intentionally left empty.
        }
    }
}
