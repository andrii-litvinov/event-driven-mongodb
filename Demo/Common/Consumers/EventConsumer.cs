using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventPublisher;
using MongoDB.Driver;
using Serilog;

namespace Common
{
    public class EventConsumer : ResilientService
    {
        private readonly Dictionary<string, Func<DomainEvent, Task>> handlers;
        private readonly string name;
        private readonly IMongoCollection<Checkpoint> checkpoints;
        private readonly IMongoCollection<EventEnvelope> events;
        private FilterDefinitionBuilder<EventEnvelope> builder;

        public EventConsumer(string name, IMongoDatabase database, Dictionary<string, Func<DomainEvent, Task>> handlers, ILogger logger) :
            base(logger)
        {
            this.name = name;
            this.handlers = handlers;
            checkpoints = database.GetCollection<Checkpoint>("checkpoints");
            events = database.GetCollection<EventEnvelope>("checkpoints");
        }

        protected override async Task Execute(CancellationToken cancellationToken)
        {
            var checkpoint = await checkpoints.Find(c => c.Name == name).FirstOrDefaultAsync(cancellationToken);
            if (checkpoint is null)
            {
                var @event = await events
                    .Find(Builders<EventEnvelope>.Filter.In("event._t", handlers.Keys))
                    .Sort(Builders<EventEnvelope>.Sort.Descending(envelope => envelope.Timestamp))
                    .FirstOrDefaultAsync(cancellationToken);

                checkpoint = new Checkpoint {Name = name, Position = @event.Timestamp};
                await checkpoints.InsertOneAsync(checkpoint, cancellationToken: cancellationToken);
            }

            builder = Builders<EventEnvelope>.Filter;

            while (!cancellationToken.IsCancellationRequested)
            {
                await events.Find(builder.And(
                        builder.In("event._t", handlers.Keys),
                        builder.Gt(envelope => envelope.Timestamp, checkpoint.Position)))
                    .ForEachAsync(async envelope =>
                    {
                        if (envelope.TryGetDomainEvent(out var @event) && handlers.TryGetValue(@event.GetType().Name, out var handler))
                            await handler.Invoke(@event);
                    }, cancellationToken);

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }
    }
}