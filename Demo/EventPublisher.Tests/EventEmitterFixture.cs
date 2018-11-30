using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using AutoFixture;
using Common;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using SimpleInjector;

namespace EventPublisher.Tests
{
    public class EventEmitterFixture : Disposable
    {
        private readonly EventEmitterService emitter;
        private readonly IFixture fixture;

        public EventEmitterFixture()
        {
            fixture = InlineServicesAttribute.CreateFixture();
            var container = fixture.Create<Container>();
            emitter = container.CreateEventEmitter(
                "test-event-emitter",
                container.GetInstance<IConfiguration>()["mongo:url"],
                new[] {"test.entities"});
        }

        public IResumeTokens Tokens => fixture.Create<IResumeTokens>();

        public IMongoCollection<BsonDocument> Entities =>
            fixture.Create<IMongoDatabase>().GetCollection<BsonDocument>("test.entities");

        private IMongoCollection<EventEnvelope> Events =>
            fixture.Create<IMongoDatabase>().GetCollection<EventEnvelope>("events");

        protected override async Task Initialize()
        {
            await emitter.StartAsync(default);
            await emitter.Started;

            OnDispose += () => emitter.StopAsync(default);
            OnDispose += () => Tokens.RemoveAll("test-event-emitter");
        }

        public async Task<EventEnvelope> GetEvent(ObjectId entityId, string type) =>
            await Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Select(_ =>
                    Events.Find(e => e.Event[PrivateField.SourceId] == entityId && e.Event["_t"] == type)
                        .FirstOrDefaultAsync())
                .Concat()
                .Where(e => e != null)
                .FirstAsync()
                .ToTask()
                .WithTimeout(10.Seconds());

        public T Create<T>() => fixture.Create<T>();
    }
}