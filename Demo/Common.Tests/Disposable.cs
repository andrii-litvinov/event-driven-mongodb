using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventPublisher.Tests
{
    public class Disposable : IAsyncLifetime
    {
        protected Func<Task> OnDispose { get; set; } = async () => { };

        Task IAsyncLifetime.InitializeAsync() => Initialize();

        async Task IAsyncLifetime.DisposeAsync()
        {
            foreach (var func in OnDispose.GetInvocationList().Cast<Func<Task>>()) await func.Invoke();
        }

        protected virtual Task Initialize() => Task.CompletedTask;
    }
}