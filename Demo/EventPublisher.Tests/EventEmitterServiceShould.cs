using System.Threading.Tasks;
using Common;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace EventPublisher.Tests
{
    public class EventEmitterServiceShould : IClassFixture<EventEmitterFixture>
    {
        public EventEmitterServiceShould(EventEmitterFixture fixture)
        {
            observable = fixture.Observable;
            calculations = fixture.Calculations;
            calculationId = fixture.Create<ObjectId>().ToString();
        }

        private readonly IEventObservable observable;
        private readonly IMongoCollection<Calculation> calculations;
        private readonly string calculationId;

        [Fact]
        public async Task EmitEventForExistingCalculation()
        {
            // Arrange
            var calculation = new Calculation(calculationId);
            calculation.Add(2);
            await calculations.Create(calculation);
            calculation.MultiplyBy(2);
            var futureEvent = observable.FirstOfType<NumberMultiplied>(calculationId);

            // Act
            await calculations.Update(calculation);

            // Assert
            calculation.Events.Should().BeEmpty();
            var @event = await futureEvent;
            @event.SourceId.Should().Be(calculationId);
            @event.Result.Should().Be(4);
        }

        [Fact]
        public async Task EmitEventForExistingCalculationOnReplace()
        {
            // Arrange
            var calculation = new Calculation(calculationId);
            calculation.Add(2);
            await calculations.Create(calculation);
            calculation = await calculations.Find(c => c.Id == calculationId).FirstAsync();
            calculation.MultiplyBy(4);
            var futureEvent = observable.FirstOfType<NumberMultiplied>(calculationId);

            // Act
            await calculations.Replace(calculation);

            // Assert
            calculation.Events.Should().BeEmpty();
            var @event = await futureEvent;
            @event.SourceId.Should().Be(calculationId);
            @event.Result.Should().Be(8);
        }

        [Fact]
        public async Task EmitEventForNewCalculation()
        {
            // Arrange
            var calculation = new Calculation(calculationId);
            calculation.Add(42);
            var futureEvent = observable.FirstOfType<NumberAdded>(calculationId);

            // Act
            await calculations.Create(calculation);

            // Assert
            calculation.Events.Should().BeEmpty();
            var @event = await futureEvent;
            @event.SourceId.Should().Be(calculationId);
            @event.Result.Should().Be(42);
        }
    }
}