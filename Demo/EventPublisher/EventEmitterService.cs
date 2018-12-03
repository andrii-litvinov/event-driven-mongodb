using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Framework;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace EventPublisher
{
    public class EventEmitterService : ResilientService
    {
        private readonly IMongoCollection<BsonDocument> events;
        private readonly ILogger logger;
        private readonly IEnumerable<string> collectionNames;
        private readonly string name;
        private readonly IOperations operations;
        private readonly TaskCompletionSource<object> started = new TaskCompletionSource<object>();
        private readonly IResumeTokens tokens;

        public EventEmitterService(string name, IMongoDatabase database, IOperations operations,
            IResumeTokens tokens, ILogger logger, IEnumerable<string> collectionNames) : base(logger)
        {
            this.name = name;
            this.operations = operations;
            this.tokens = tokens;
            this.logger = logger;
            this.collectionNames = collectionNames;
            events = database.GetCollection<BsonDocument>("events");
        }

        public Task Started => started.Task;

        protected override async Task Execute(CancellationToken cancellationToken)
        {
            var resumeToken = await tokens.Get(name, cancellationToken);
            var cursor = await operations.GetCursor(resumeToken, collectionNames, cancellationToken);

            started.SetResult(null);

            await Observable
                .Create<BatchItem>(observer => cursor.ForEachAsync(
                    operation => EmitEvent(observer, operation), cancellationToken))
                .Buffer(TimeSpan.FromMilliseconds(100), 1000)
                .Select(items => SaveEvents(items, resumeToken))
                .Concat();
        }

        private static async Task EmitEvent(IObserver<BatchItem> observer, BsonDocument operation)
        {
            var @object = (BsonDocument) operation["o"];
            switch ((string) operation["op"])
            {
                case "i":
                {
                    EmitDomainEvents(@object);
                    break;
                }
                case "u":
                {
                    if (@object.TryGetValue("$set", out var set))
                        EmitDomainEvents((BsonDocument) set);
                    else
                        EmitDomainEvents(@object);
                    break;
                }
            }

            void EmitDomainEvents(BsonDocument document)
            {
                if (document.TryGetValue(PrivateField.Events, out var e) && e is BsonArray embeddedEvents)
                    foreach (var @event in embeddedEvents.Cast<BsonDocument>())
                        OnNext(@event);
            }

            void OnNext(BsonDocument envelope) => observer.OnNext(new BatchItem
            {
                Envelope = envelope,
                Token = new BsonDocument {{"ts", (BsonTimestamp) operation["ts"]}, {"h", operation["h"]}},
                WallClock = (DateTime) operation["wall"]
            });
        }

        private async Task<Unit> SaveEvents(ICollection<BatchItem> items, ResumeToken resumeToken)
        {
            if (items.Any())
            {
                var last = items.Last();

                await events.InsertManyAsync(items.Select(item => item.Envelope));

                resumeToken.Token = last.Token;
                resumeToken.Updated = last.WallClock;

                await tokens.Save(resumeToken);

                logger.Information("Published {@count} event(s).", items.Count);
            }

            return Unit.Default;
        }

        private class BatchItem
        {
            public BsonDocument Envelope { get; set; }
            public BsonDocument Token { get; set; }
            public DateTime WallClock { get; set; }
        }
    }
}