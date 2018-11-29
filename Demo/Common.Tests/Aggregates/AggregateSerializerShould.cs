using System.Threading.Tasks;
using EventPublisher.Tests;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Common.Tests
{
    public class AggregateSerializerShould
    {
        [Theory, InlineServices]
        public async Task NotSerializeEmptyEvents(ObjectId id, IMongoDatabase database)
        {
            // Arrange
            var aggregates = database.GetCollection<TestAggregate>("test.aggregates");
            var bsonAggregates = database.GetCollection<BsonDocument>("test.aggregates");
            var aggregate = new TestAggregate(id.ToString());

            // Act
            await aggregates.InsertOneAsync(aggregate);

            // Assert
            var bsonAggregate = await bsonAggregates.Find(document => document["_id"] == id).FirstAsync();
            bsonAggregate.TryGetValue("events", out _).Should().BeFalse();

            var aggregate1 = await aggregates.Find(a => a.Id == aggregate.Id).FirstAsync();
            aggregate1.Events.Should().BeNull();
        }

        [Theory, InlineServices]
        public async Task SerializeEvents(ObjectId id, IMongoDatabase database)
        {
            // Arrange
            var aggregates = database.GetCollection<TestAggregate>("test.aggregates");
            var bsonAggregates = database.GetCollection<BsonDocument>("test.aggregates");
            var aggregate = new TestAggregate(id.ToString());
            aggregate.RecordEvent(new AggregateModified(aggregate.Id));

            // Act
            await aggregates.InsertOneAsync(aggregate);

            // Assert
            var bsonAggregate = await bsonAggregates.Find(document => document["_id"] == id).FirstAsync();
            bsonAggregate.TryGetValue(PrivateField.Events, out var events).Should().BeTrue();
            events.AsBsonArray.Should().HaveCount(1);
        }

        [Theory, InlineServices]
        public async Task NotDeserializeEvents(ObjectId id, IMongoDatabase database)
        {
            var aggregates = database.GetCollection<TestAggregate>("test.aggregates");
            var aggregate = new TestAggregate(id.ToString());
            aggregate.RecordEvent(new AggregateModified(aggregate.Id));

            // Act
            await aggregates.InsertOneAsync(aggregate);

            // Assert
            var aggregate1 = await aggregates.Find(a => a.Id == aggregate.Id).FirstAsync();
            aggregate1.Events.Should().BeNull();
        }
    }
}