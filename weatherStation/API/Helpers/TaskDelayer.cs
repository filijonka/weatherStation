using API.Interface.Helpers;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Helpers;

/// <inheritdoc />
public class TaskDelayer : ITaskDelayer
{
    /// <inheritdoc />
    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
    }
}