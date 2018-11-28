using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Domain;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using SimpleInjector;
using Xunit;

namespace EventPublisher.Tests
{
    public class EventEmitterServiceShould : IClassFixture<EventEmitterServiceShould.Fixture>
    {
        public EventEmitterServiceShould(Fixture fixture) => this.fixture = fixture;
        private readonly FilterDefinitionBuilder<BsonDocument> filter = Builders<BsonDocument>.Filter;
        private readonly Fixture fixture;
        private readonly UpdateDefinitionBuilder<BsonDocument> update = Builders<BsonDocument>.Update;

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
                Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .Select(_ => Events.Find(e => e.Event[PrivateField.SourceId] == entityId && e.Event["_t"] == type)
                        .FirstOrDefaultAsync())
                    .Concat()
                    .Where(e => e != null)
                    .Subscribe(e => tcs.TrySetResult(e));

                Task.Delay(10.Seconds()).ContinueWith(task => tcs.TrySetException(new TimeoutException()));

                return tcs.Task;
            }

            public T Create<T>() => fixture.Create<T>();
        }

        [Fact]
        public async Task EmitCreatedEvent()
        {
            // Arrange
            var entityId = fixture.Create<ObjectId>();
            var entity = new BsonDocument {{"_id", entityId}};

            // Act
            await fixture.Entities.InsertOneAsync(entity);

            // Assert
            var envelope = await fixture.GetEvent(entityId, "EntityCreated");
            envelope.EventId.Should().NotBeNullOrEmpty();

            var @event = envelope.Event;
            @event["_t"].Should().Be("EntityCreated");
            @event[PrivateField.SourceId].Should().Be(entityId);
            ((BsonDocument) @event["entity"]).Should().Equal(entity);
        }

        [Fact]
        public async Task EmitUpdatedEvent()
        {
            // Arrange
            var entityId = fixture.Create<ObjectId>();
            await fixture.Entities.InsertOneAsync(new BsonDocument {{"_id", entityId}, {"value", true}, {"_t", "Entity"}});

            // Act
            await fixture.Entities.UpdateOneAsync(filter.Eq("_id", entityId), update.Set("field.value", true));

            // Assert
            var envelope = await fixture.GetEvent(entityId, "EntityUpdated");
            envelope.EventId.Should().NotBeNullOrEmpty();

            var @event = envelope.Event;
            @event["_t"].Should().Be("EntityUpdated");
            @event[PrivateField.SourceId].Should().Be(entityId);
            @event["field.value"].Should().Be(true);
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