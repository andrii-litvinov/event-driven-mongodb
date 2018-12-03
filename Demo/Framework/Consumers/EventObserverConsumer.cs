using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

// ReSharper disable once MethodSupportsCancellation

namespace Framework
{
    public class EventObserverConsumer : ResilientService
    {
        private readonly IMongoCollection<Checkpoint> checkpoints;
        private readonly IMongoCollection<EventEnvelope> events;
        private readonly string name;
        private readonly IEventObservable observable;

        public EventObserverConsumer(string name, IMongoDatabase database, ILogger logger,
            IEventObservable observable) : base(logger)
        {
            this.name = name;
            this.observable = observable;
            checkpoints = database.GetCollection<Checkpoint>("checkpoints");
            events = database.GetCollection<EventEnvelope>("events");
        }

        protected override async Task Execute(CancellationToken cancellationToken)
        {
            var checkpoint = await GetCheckpoint(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                await events
                    .Find(envelope => envelope.Timestamp > checkpoint.Position)
                    .Sort(Builders<EventEnvelope>.Sort.Ascending(envelope => envelope.Timestamp))
                    .ForEachAsync(async envelope =>
                    {
                        observable.Publish(envelope.Event);
                        await SaveCheckpoint(checkpoint, envelope.Timestamp);
                    }, cancellationToken);

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }

        private async Task<Checkpoint> GetCheckpoint(CancellationToken cancellationToken)
        {
            try
            {
                var latestEvent = await events
                    .Find(Builders<EventEnvelope>.Filter.Empty)
                    .Sort(Builders<EventEnvelope>.Sort.Descending(envelope => envelope.Timestamp))
                    .FirstOrDefaultAsync();

                var checkpoint = await checkpoints.Find(c => c.Name == name).FirstOrDefaultAsync(cancellationToken);
                if (checkpoint is null)
                {
                    checkpoint = new Checkpoint {Name = name};
                    await checkpoints.InsertOneAsync(checkpoint, cancellationToken: cancellationToken);
                }

                checkpoint.Position = latestEvent?.Timestamp ?? new BsonTimestamp(0, 0);
                return checkpoint;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task SaveCheckpoint(Checkpoint checkpoint, BsonTimestamp position)
        {
            checkpoint.Position = position;
            await checkpoints.UpdateOneAsync(
                c => c.Id == checkpoint.Id,
                Builders<Checkpoint>.Update.Set(c => c.Position, checkpoint.Position));
        }
    }
}