using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Serilog;

namespace EventPublisher
{
    public class EventEmitterService : ResilientService
    {
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<BsonDocument> events;
        private readonly IDictionary<string, string> map;
        private readonly string name;
        private readonly IOperations operations;
        private readonly IResumeTokens tokens;
        private readonly TaskCompletionSource<object> started = new TaskCompletionSource<object>();

        public EventEmitterService(string name, IMongoDatabase database, IOperations operations,
            IResumeTokens tokens, ILogger logger, IDictionary<string, string> map) : base(logger)
        {
            this.name = name;
            this.operations = operations;
            this.database = database;
            this.tokens = tokens;
            this.map = map;
            events = database.GetCollection<BsonDocument>("events");
        }

        public Task Started => started.Task;

        protected override async Task Execute(CancellationToken cancellationToken)
        {
            var resumeToken = await tokens.Get(name, cancellationToken);
            var cursor = await operations.GetCursor(resumeToken, map.Keys, cancellationToken);
            
            started.SetResult(null);

            await Observable
                .Create<BatchItem>(observer => cursor.ForEachAsync(
                    operation => EmitEvent(observer, operation, resumeToken), cancellationToken))
                .Buffer(TimeSpan.FromSeconds(1), 1000)
                .Select(items => SaveEvents(items, resumeToken))
                .Concat();
        }

        private async Task EmitEvent(IObserver<BatchItem> observer, BsonDocument operation, ResumeToken resumeToken)
        {
            var @namespace = (string) operation["ns"];
            var collectionName = @namespace.Substring(@namespace.IndexOf(".", StringComparison.Ordinal) + 1);

            switch ((string) operation["op"])
            {
                case "i":
                {
                    var document = (BsonDocument) operation["o"];

                    if (TryEmitEmbeddedDomainEvents(document)) return;

                    var trace = GetTrace(document);
                    var type = EventTypeFactory.Create(document, ChangeStreamOperationType.Insert, map[collectionName]);
                    var @event = new BsonDocument
                    {
                        {"_t", type},
                        {PrivateField.SourceId, document["_id"]},
                        {"entity", document}
                    };
                    OnNext(CreateEnvelope(@event, trace).ToBsonDocument());

                    break;
                }
                case "u":
                {
                    var documentKey = new BsonDocument("_id", operation["o2"]["_id"]);

                    var obj = (BsonDocument) operation["o"];
                    var commands = obj.Names.Where(n => n.StartsWith("$")).ToArray();

                    if (commands.Any())
                    {
                        foreach (var command in commands)
                            switch (command)
                            {
                                case "$v":
                                case "$unset":
                                    break;
                                case "$set":
                                    var @event = (BsonDocument) obj["$set"];
                                    if (TryEmitEmbeddedDomainEvents(@event))
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        var collection = database.GetCollection<BsonDocument>(collectionName);
                                        var document = await collection
                                            .Find(documentKey)
                                            .Project(new BsonDocument("_t", 1))
                                            .FirstOrDefaultAsync();
                                        var type = EventTypeFactory.Create(document, ChangeStreamOperationType.Update, map[collectionName]);
                                        @event.Add("_t", type);
                                        @event.Add(PrivateField.SourceId, documentKey["_id"]);
                                        var trace = GetTrace(@event);

                                        OnNext(CreateEnvelope(@event, trace).ToBsonDocument());
                                    }

                                    break;
                                default:
                                    throw new Exception(
                                        $"Command {command} is not recognized. Timestamp: {operation["ts"]}, hash: {operation["h"]}.");
                            }
                    }
                    else
                    {
                        if (TryEmitEmbeddedDomainEvents(obj)) return;

                        var trace = GetTrace(obj);
                        var type = EventTypeFactory.Create(obj, ChangeStreamOperationType.Replace, map[collectionName]);
                        var @event = new BsonDocument
                        {
                            {"_t", type},
                            {PrivateField.SourceId, documentKey["_id"]},
                            {"entity", obj}
                        };

                        OnNext(CreateEnvelope(@event, trace).ToBsonDocument());
                    }

                    break;
                }
                case "d":
                {
                    var documentKey = (BsonDocument) operation["o"];
                    var type = EventTypeFactory.Create(new BsonDocument(), ChangeStreamOperationType.Delete, map[collectionName]);
                    var @event = new BsonDocument {{"_t", type}, {PrivateField.SourceId, documentKey["_id"]}};

                    OnNext(CreateEnvelope(@event, null).ToBsonDocument());
                    break;
                }
                default:
                    throw new Exception(
                        $"Unsupported operation type {operation["op"]} encountered. Timestamp: {operation["ts"]}, hash: {operation["h"]}");
            }

            bool TryEmitEmbeddedDomainEvents(BsonDocument document)
            {
                if (document.TryGetValue(PrivateField.Events, out var evts) && evts is BsonArray embeddedEvents)
                {
                    foreach (var @event in embeddedEvents.Cast<BsonDocument>()) OnNext(@event);
                    return true;
                }

                return false;
            }

            void OnNext(BsonDocument envelope)
            {
                observer.OnNext(new BatchItem
                {
                    Envelope = envelope,
                    Token = new BsonDocument {{"ts", operation["ts"]}, {"h", operation["h"]}},
                    WallClock = (DateTime) operation["wall"]
                });
            }
        }

        private static BsonEnvelope CreateEnvelope(BsonDocument @event, Trace trace)
        {
            return new BsonEnvelope
            {
                EventId = trace?.Id ?? Guid.NewGuid().ToString(),
                Event = @event,
                CorrelationId = trace?.CorrelationId,
                CausationId = trace?.CausationId
            };
        }

        private static Trace GetTrace(BsonDocument entity)
        {
            if (entity.TryGetValue(PrivateField.Trace, out var t))
            {
                entity.Remove(PrivateField.Trace);
                return BsonSerializer.Deserialize<Trace>((BsonDocument) t);
            }

            return null;
        }

        private async Task<Unit> SaveEvents(IList<BatchItem> items, ResumeToken resumeToken)
        {
            if (items.Any())
            {
                var last = items.Last();

                await events.InsertManyAsync(items.Select(item => item.Envelope));

                resumeToken.Token = last.Token;
                resumeToken.Updated = last.WallClock;

                await tokens.Save(resumeToken);
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
    
    public class BsonEnvelope
    {
        public BsonDocument Event { get; set; }
        public string EventId { get; set; }
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }
    }
}