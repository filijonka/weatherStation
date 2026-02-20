using System;
using Microsoft.AspNetCore.Mvc;

namespace API.Filters;

/// <summary>
/// Our authenticatin attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class NetatmoAuthenticatedAttribute : TypeFilterAttribute
    
{
    /// <inheritdoc />
    public NetatmoAuthenticatedAttribute() : base(typeof(NetatmoAuthenticatedFilter))
    {
    }
}