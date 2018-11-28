using System.Threading.Tasks;
using Domain;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace EventPublisher.Tests
{
    public class EventEmitterServiceShould : IClassFixture<EventEmitterFixture>
    {
        public EventEmitterServiceShould(EventEmitterFixture fixture) => this.fixture = fixture;
        private readonly FilterDefinitionBuilder<BsonDocument> filter = Builders<BsonDocument>.Filter;
        private readonly EventEmitterFixture fixture;
        private readonly UpdateDefinitionBuilder<BsonDocument> update = Builders<BsonDocument>.Update;

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

        [Fact]
        public async Task EmitDeletedEvent()
        {
            // Arrange
            var entityId = fixture.Create<ObjectId>();
            await fixture.Entities.InsertOneAsync(new BsonDocument {{"_id", entityId}, {"_t", "Entity"}});

            // Act
            await fixture.Entities.DeleteOneAsync(filter.Eq("_id", entityId));

            // Assert
            var envelope = await fixture.GetEvent(entityId, "EntityDeleted");
            envelope.EventId.Should().NotBeNullOrEmpty();

            var @event = envelope.Event;
            @event["_t"].Should().Be("EntityDeleted");
            @event[PrivateField.SourceId].Should().Be(entityId);
        }
    }
}