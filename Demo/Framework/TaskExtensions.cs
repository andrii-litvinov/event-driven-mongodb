using System;
using System.Threading;
using System.Threading.Tasks;

namespace Framework
{
    public static class TaskExtensions
    {
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));
            if (completed == task) return await task;
            throw new TimeoutException();
        }
    }
}