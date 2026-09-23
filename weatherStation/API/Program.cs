using API.Auth.Netatmo.Options;
using API.Extensions;
using API.Options;
using Asp.Versioning.ApiExplorer;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using System.Diagnostics.CodeAnalysis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ILogger applicationLogger = ApplicationServiceExtension.CreateLogger(builder.Configuration);
builder.Host.UseSerilog(applicationLogger);
builder.Services.AddSingleton(applicationLogger);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    options.TagActionsBy(api =>
    {
        if (api.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
        {
            return [api.RelativePath];
        }

        string controllerName = controllerActionDescriptor.ControllerName;
        if (api.RelativePath == null)
        {
            return [controllerName];
        }

        string[] pathSegments = api.RelativePath.Split('/');
        if (pathSegments.Length <= 3 || !pathSegments[2].Equals(controllerName, StringComparison.OrdinalIgnoreCase) ||
            pathSegments[3].Contains('{'))
        {
            return [controllerName];
        }

        string subGroupName = pathSegments[3];

        subGroupName = char.ToUpper(subGroupName[0]) + subGroupName[1..];

        return [$"{controllerName} - {subGroupName}"];
    });
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

WebApplication app = builder.Build();

try
{
    _ = app.Services.GetRequiredService<IOptions<NetatmoOptions>>().Value;
    _ = app.Services.GetRequiredService<IOptions<WeatherApiOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    foreach (string failure in ex.Failures)
        applicationLogger.Fatal("Options validation failed: {Failure}", failure);
    throw;
}

// Configure the HTTP request pipeline.
IApiVersionDescriptionProvider apiVersionDescriptionProvider =
    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    IReadOnlyList<ApiVersionDescription> descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions;
    foreach (string groupName in descriptions.Select(description => description.GroupName))
    {
        options.SwaggerEndpoint($"/swagger/{groupName}/swagger.json", groupName.ToUpperInvariant());
    }
});

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseResponseCaching();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>
/// Application entry point.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class Program
{
}
