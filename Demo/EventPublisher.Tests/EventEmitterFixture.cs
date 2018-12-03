using System.Threading.Tasks;
using AutoFixture;
using Common;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Orders;
using Serilog;
using SimpleInjector;

namespace EventPublisher.Tests
{
    public class EventEmitterFixture : Disposable
    {
        private readonly EventEmitterService emitter;
        private readonly IFixture fixture;
        private readonly EventObserverConsumer consumer;
        private readonly EventObservable observable;

        public EventEmitterFixture()
        {
            fixture = InlineServicesAttribute.CreateFixture();
            var container = fixture.Create<Container>();
            emitter = container.CreateEventEmitter(
                "test-event-emitter",
                container.GetInstance<IConfiguration>()["mongo:url"],
                new[] {"test.entities"});

            observable = new EventObservable();
            consumer = new EventObserverConsumer(
                "test-event-observer",
                fixture.Create<IMongoDatabase>(),
                fixture.Create<ILogger>(),
                observable);
        }

        public IResumeTokens Tokens => fixture.Create<IResumeTokens>();
        public IEventObservable Observable => observable;

        public IMongoCollection<Calculation> Calculations =>
            fixture.Create<IMongoDatabase>().GetCollection<Calculation>("test.entities");

        protected override async Task Initialize()
        {
            await consumer.StartAsync(default);
            await emitter.StartAsync(default);
            await emitter.Started;

            OnDispose += () => emitter.StopAsync(default);
            OnDispose += () => consumer.StopAsync(default);
            OnDispose += () => Tokens.RemoveAll("test-event-emitter");
        }

        public T Create<T>() => fixture.Create<T>();
    }
}