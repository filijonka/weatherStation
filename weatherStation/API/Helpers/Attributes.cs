using System;
namespace API.Helpers;

/// <summary>
/// Adding the ApiKeyAuth as an attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthAttribute : Attribute
{
}