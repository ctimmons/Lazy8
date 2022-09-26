using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

[TestFixture]
public class RateLimiterTests
{
  private static readonly Random _random = new();

  [Test]
  public async Task WaitAsyncTest_StartAllTasksAsQuicklyAsPossible()
  {
    /* Start N tasks as quickly as possible to see if RateLimiter
       actually limits the concurrent nubmer of tasks. */

    await WaitAsyncTest(numberOfTasks: 100, delay: () => Task.CompletedTask);
  }

  [Test]
  public async Task WaitAsyncTest_StartTasksAtRandomIntervals()
  {
    /* Start N tasks at random intervals. */

    const Int32 shortestTaskStartDelay = 500;
    const Int32 longestTaskStartDelay = 2500;

    await WaitAsyncTest(numberOfTasks: 20, delay: () => Task.Delay(_random.Next(shortestTaskStartDelay, longestTaskStartDelay)));
  }
  
  private async Task WaitAsyncTest(Int32 numberOfTasks, Func<Task> delay)
  {
    const Int32 maximumNumberOfTasksPerTimeSpan = 10;
    var timeSpanInSeconds = TimeSpan.FromSeconds(1);

    List<Task> taskList = new();
    /* Have to use a concurrent queue because the List<T>.Add() method isn't threadsafe. */
    ConcurrentQueue<TimeSpan> startTimes = new();
    RateLimiter rateLimiter = new(maximumNumberOfTasksPerTimeSpan, timeSpanInSeconds);

    Stopwatch sw = new();
    sw.Start();

    foreach (var _ in Enumerable.Range(1, numberOfTasks))
    {
      await delay();

      await rateLimiter.WaitAsync();
      taskList.Add(Task.Factory.StartNew(
        async () =>
        {
          startTimes.Enqueue(sw.Elapsed);

          /* The task lifetime doesn't matter as rate limiting is only concerned
             with how many tasks *start* within a given time span. */

          await Task.Delay(100);
        }));
    }

    Task.WaitAll(taskList.ToArray());

    sw.Stop();

    foreach (var group in startTimes.GroupBy(startTime => startTime.Seconds))
      Assert.That(group.Count() <= maximumNumberOfTasksPerTimeSpan);
  }
}

