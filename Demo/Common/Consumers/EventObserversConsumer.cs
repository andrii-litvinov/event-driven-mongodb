using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace Common
{
    public class EventObserversConsumer : ResilientService
    {
        private readonly IMongoCollection<Checkpoint> checkpoints;
        private readonly IMongoCollection<EventEnvelope> events;
        private readonly IEventObservables observables;
        private readonly string name;
        private readonly FilterDefinitionBuilder<EventEnvelope> builder = Builders<EventEnvelope>.Filter;

        public EventObserversConsumer(string name, IMongoDatabase database, ILogger logger, IEventObservables observables) : base(logger)
        {
            this.name = name;
            this.observables = observables;
            checkpoints = database.GetCollection<Checkpoint>("checkpoints");
            events = database.GetCollection<EventEnvelope>("events");
        }

        protected override async Task Execute(CancellationToken cancellationToken)
        {
            var checkpoint = await checkpoints.Find(c => c.Name == name).FirstOrDefaultAsync(cancellationToken);
            if (checkpoint is null)
            {
                var @event = await events
                    .Find(Builders<EventEnvelope>.Filter.Empty)
                    .Sort(Builders<EventEnvelope>.Sort.Descending(envelope => envelope.Timestamp))
                    .FirstOrDefaultAsync(cancellationToken);

                checkpoint = new Checkpoint {Name = name, Position = @event?.Timestamp ?? new BsonTimestamp(0, 0)};
                await checkpoints.InsertOneAsync(checkpoint, cancellationToken: cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await events
                    .Find(envelope => envelope.Timestamp > checkpoint.Position)
                    .Sort(Builders<EventEnvelope>.Sort.Ascending(envelope => envelope.Timestamp))
                    .ForEachAsync(async envelope =>
                    {
                        if (envelope.TryGetDomainEvent(out var @event)) observables.Publish(@event);

                        checkpoint.Position = envelope.Timestamp;
                        await checkpoints.UpdateOneAsync(
                            c => c.Id == checkpoint.Id,
                            Builders<Checkpoint>.Update.Set(c => c.Position, checkpoint.Position));
                    }, cancellationToken);

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }
    }
}