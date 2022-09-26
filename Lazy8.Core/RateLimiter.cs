using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lazy8.Core;

/* Code for RateLimiter is from StackOverflow answer https://stackoverflow.com/a/65829971/116198
   posted by user Theodor Zoulias (https://stackoverflow.com/users/11178549/theodor-zoulias).

   Modifications:

     I made the private ScheduleSemaphoreRelease() method observe the same
     CancellationToken the public WaitAsync() method watches.
     This way, if the WaitAsync() method is cancelled, the asynchronous delay in the private
     ScheduleSemaphoreRelease() method is also cancelled.

   Licensed under CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/)
   See https://stackoverflow.com/help/licensing for more info. */

public class RateLimiter
{
  private readonly SemaphoreSlim _semaphore;
  private readonly TimeSpan _timeUnit;

  public RateLimiter(Int32 maxActionsPerTimeUnit, TimeSpan timeUnit)
  {
    if (maxActionsPerTimeUnit < 1)
      throw new ArgumentOutOfRangeException(nameof(maxActionsPerTimeUnit));

    if ((timeUnit < TimeSpan.Zero) || (timeUnit.TotalMilliseconds > Int32.MaxValue))
      throw new ArgumentOutOfRangeException(nameof(timeUnit));

    this._semaphore = new SemaphoreSlim(maxActionsPerTimeUnit, maxActionsPerTimeUnit);
    this._timeUnit = timeUnit;
  }

  public async Task WaitAsync(CancellationToken cancellationToken = default)
  {
    await this._semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    this.ScheduleSemaphoreRelease(cancellationToken);
  }

  private async void ScheduleSemaphoreRelease(CancellationToken cancellationToken)
  {
    try
    {
      await Task.Delay(_timeUnit, cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      this._semaphore.Release();
    }
  }
}
