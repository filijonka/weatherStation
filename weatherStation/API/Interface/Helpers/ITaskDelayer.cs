using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Interface.Helpers;

/// <summary>
/// A wrapper for delay
/// </summary>
public interface ITaskDelayer
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="delay"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Delay(TimeSpan delay, CancellationToken cancellationToken);
}