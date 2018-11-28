using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Domain;
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
                new Dictionary<string, string> {{"test.entities", "Entity"}});
        }

        public IResumeTokens Tokens => fixture.Create<IResumeTokens>();
        public IMongoCollection<BsonDocument> Entities => fixture.Create<IMongoDatabase>().GetCollection<BsonDocument>("test.entities");
        private IMongoCollection<BsonEnvelope> Events => fixture.Create<IMongoDatabase>().GetCollection<BsonEnvelope>("events");

        protected override async Task Initialize()
        {
            await emitter.StartAsync(default);
            await emitter.Started;

            OnDispose += () => emitter.StopAsync(default);
            OnDispose += () => Tokens.RemoveAll("test-event-emitter");
        }

        public Task<BsonEnvelope> GetEvent(ObjectId entityId, string type)
        {
            var tcs = new TaskCompletionSource<BsonEnvelope>();
            IDisposable subscription = null;
            subscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Select(_ => Events.Find(e => e.Event[PrivateField.SourceId] == entityId && e.Event["_t"] == type)
                    .FirstOrDefaultAsync())
                .Concat()
                .Where(e => e != null)
                .Subscribe(e =>
                {
                    tcs.TrySetResult(e);
                    // ReSharper disable once AccessToModifiedClosure
                    subscription?.Dispose();
                });

            Task.Delay(10.Seconds()).ContinueWith(task => tcs.TrySetException(new TimeoutException()));

            return tcs.Task;
        }

        public T Create<T>() => fixture.Create<T>();
    }
}