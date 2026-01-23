using API.Helpers;

using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using System;
namespace API.Extensions;

/// <summary>
/// Configures Swagger generation for versioned APIs.
/// </summary>
public sealed class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
    /// </summary>
    /// <param name="provider">API version description provider.</param>
    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        this.provider = provider;
    }

    /// <summary>
    /// Configure each API discovered for Swagger Documentation
    /// </summary>
    /// <param name="options">Swagger generation options.</param>
    public void Configure(SwaggerGenOptions options)
    {
        // Add security definition for API Key
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API Key authentication using X-Api-Key header",
            Name = "X-Api-Key",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiKeyScheme"
        });

        // Add security requirement filter for endpoints with [ApiKeyAuth] attribute
        options.OperationFilter<ApiKeyAuthOperationFilter>();

        // add swagger document for every API version discovered
        foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                CreateVersionInfo(description));
        }
    }

    /// <summary>
    /// Configure Swagger Options. Inherited from the Interface
    /// </summary>
    /// <param name="name">Options name.</param>
    /// <param name="options">Swagger generation options.</param>
    public void Configure(string name, SwaggerGenOptions options)
    {
        Configure(options);
    }
    
    /// <summary>
    /// Creates OpenAPI document metadata for a version.
    /// </summary>
    /// <param name="description">API version description.</param>
    /// <returns>OpenAPI metadata.</returns>
    private static OpenApiInfo CreateVersionInfo(ApiVersionDescription description)
    {
        OpenApiInfo info = new OpenApiInfo
        {
            Version = "v1",
            Title = "Weather Station",
            Description = "",
        };

        if (description.IsDeprecated)
        {
            info.Description += " This API version has been deprecated. Please use one of the new APIs available from the explorer.";
        }

        return info;
    }
}

/// <summary>
/// Operation filter to add security requirement for endpoints with <see cref="ApiKeyAuthAttribute"/>.
/// </summary>
public class ApiKeyAuthOperationFilter : IOperationFilter
{
    /// <summary>
    /// Apply security requirement to operations with [ApiKeyAuth] attribute
    /// </summary>
    /// <param name="operation">OpenAPI operation.</param>
    /// <param name="context">Operation filter context.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo == null)
        {
            return;
        }

        // Check if the method or controller has [ApiKeyAuth] attribute
        bool hasApiKeyAuth = context.MethodInfo.GetCustomAttributes(typeof(ApiKeyAuthAttribute), true).Any() ||
                             (context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(ApiKeyAuthAttribute), true).Any() ?? false);

        if (hasApiKeyAuth)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}