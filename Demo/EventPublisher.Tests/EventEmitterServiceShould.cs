using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Configuration;
using SimpleInjector;
using Xunit;

namespace EventPublisher.Tests
{
    public class EventEmitterServiceShould : IClassFixture<EventEmitterServiceShould.Fixture>
    {
        public EventEmitterServiceShould(Fixture fixture) => this.fixture = fixture;
        private readonly Fixture fixture;

        public class Fixture : Disposable
        {
            private readonly EventEmitterService emitter;
            private readonly IFixture fixture;

            public Fixture()
            {
                fixture = InlineServicesAttribute.CreateFixture();
                var container = fixture.Create<Container>();
                emitter = container.CreateEventEmitter(
                    "test-event-emitter",
                    container.GetInstance<IConfiguration>()["mongo:url"],
                    new Dictionary<string, string> {{"test.entities", "Entity"}});
            }

            public IResumeTokens Tokens => fixture.Create<IResumeTokens>();

            protected override async Task Initialize()
            {
                await emitter.StartAsync(default);
                await emitter.Started;

                OnDispose += () => emitter.StopAsync(default);
                OnDispose += () => Tokens.RemoveAll("test-event-emitter");
            }

//            public Task<Event> GetEvent(ObjectId entityId, string type)
//            {
//                var tcs = new TaskCompletionSource<Event>();
//                Observable
//                    .Interval(TimeSpan.FromSeconds(1))
//                    .Select(_ => Events.Find(e => e.Body.DocumentKey["_id"] == entityId && e.Type == type).FirstOrDefaultAsync())
//                    .Concat()
//                    .Where(e => e != null)
//                    .Subscribe(e2 => tcs.TrySetResult(e2));
//
//                Task.Delay(10.Seconds()).ContinueWith(task => tcs.TrySetException(new TimeoutException()));
//
//                return tcs.Task;
//            }

//            public Task<List<Event>> GetEvents(ObjectId id, string type)
//            {
//                var tcs = new TaskCompletionSource<List<Event>>();
//                Observable
//                    .Interval(TimeSpan.FromSeconds(1))
//                    .Select(_ => Events.Find(e => e.Body.DocumentKey["_id"] == id && e.Type == type).ToListAsync())
//                    .Concat()
//                    .Where(events => events.Any())
//                    .Subscribe(events => tcs.TrySetResult(events));
//
//                Task.Delay(10.Seconds()).ContinueWith(task => tcs.TrySetException(new TimeoutException()));
//
//                return tcs.Task;
//            }

            public T Create<T>() => fixture.Create<T>();
        }

        [Fact]
        public async Task ShouldEmitCreatedEvent()
        {
        }
    }

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