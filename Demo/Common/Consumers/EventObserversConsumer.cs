using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

// ReSharper disable once MethodSupportsCancellation

namespace Common
{
    public class EventObserversConsumer : ResilientService
    {
        private readonly IMongoCollection<Checkpoint> checkpoints;
        private readonly IMongoCollection<EventEnvelope> events;
        private readonly string name;
        private readonly IEventObservable observable;

        public EventObserversConsumer(string name, IMongoDatabase database, ILogger logger,
            IEventObservable observable) : base(logger)
        {
            this.name = name;
            this.observable = observable;
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
                        try
                        {
                            if (envelope.TryGetDomainEvent(out var @event)) observable.Publish(@event);
                        }
                        catch (BsonSerializationException)
                        {
                            // Event that occured is not defined by any references dll, so skipping. 
                        }

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