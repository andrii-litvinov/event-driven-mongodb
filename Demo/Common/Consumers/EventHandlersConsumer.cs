using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
// ReSharper disable once MethodSupportsCancellation

namespace Common
{
    using Handlers = Dictionary<string, Func<DomainEvent, Task>>;

    public class EventHandlersConsumer : ResilientService
    {
        private readonly IMongoCollection<Checkpoint> checkpoints;
        private readonly IMongoCollection<EventEnvelope> events;
        private readonly Dictionary<string, Func<DomainEvent, Task>> handlers;
        private readonly string name;
        private readonly FilterDefinitionBuilder<EventEnvelope> builder = Builders<EventEnvelope>.Filter;

        public EventHandlersConsumer(string name, IMongoDatabase database, Handlers handlers, ILogger logger) : base(logger)
        {
            this.name = name;
            this.handlers = handlers;
            checkpoints = database.GetCollection<Checkpoint>("checkpoints");
            events = database.GetCollection<EventEnvelope>("events");
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

                checkpoint = new Checkpoint {Name = name, Position = @event?.Timestamp ?? new BsonTimestamp(0, 0)};
                await checkpoints.InsertOneAsync(checkpoint, cancellationToken: cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await events
                    .Find(builder.And(
                        builder.In("event._t", handlers.Keys),
                        builder.Gt(envelope => envelope.Timestamp, checkpoint.Position)))
                    .Sort(Builders<EventEnvelope>.Sort.Ascending(envelope => envelope.Timestamp))
                    .ForEachAsync(async envelope =>
                    {
                        if (envelope.TryGetDomainEvent(out var @event))
                        {
                            if (handlers.TryGetValue(@event.GetType().Name, out var handler))
                                foreach (Func<DomainEvent, Task> @delegate in handler.GetInvocationList())
                                    await @delegate.Invoke(@event);
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