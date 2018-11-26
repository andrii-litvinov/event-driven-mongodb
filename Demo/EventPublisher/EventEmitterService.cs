using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace EventPublisher
{
    // TODO: Implement simple endpoint to receive POST requests to create new entity. 
    
    public class EventEmitterService : ResilientService
    {
        private readonly IMongoDatabase database;
        private readonly IDictionary<string, string> collectionMap;
        private readonly string name;
        private readonly IMongoCollection<EventEnvelope> events;
        private readonly IOperations operations;
        private readonly IResumeTokens tokens;

        public EventEmitterService(string name, IMongoDatabase database, IOperations operations,
            IResumeTokens tokens, ILogger logger, IDictionary<string, string> collectionMap) : base(logger)
        {
            this.name = name;
            this.operations = operations;
            this.database = database;
            this.tokens = tokens;
            this.collectionMap = collectionMap;
            events = database.GetCollection<EventEnvelope>("events");
        }

        protected override async Task Execute(CancellationToken cancellationToken)
        {
            var resumeToken = await tokens.Get(name, cancellationToken);
            var cursor = await operations.GetCursor(resumeToken, collectionMap.Keys, cancellationToken);

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
            ChangeStreamOperationType operationType;
            BsonDocument documentKey;
            BsonDocument fullDocument = null;
            BsonDocument updatedFields = null;

            switch ((string) operation["op"])
            {
                case "i":
                    operationType = ChangeStreamOperationType.Insert;
                    documentKey = new BsonDocument("_id", operation["o"]["_id"]);
                    fullDocument = (BsonDocument) operation["o"];

                    if (TryEmitDomainEvents(fullDocument)) return;

                    break;
                case "u":
                    operationType = ChangeStreamOperationType.Update;
                    documentKey = new BsonDocument("_id", operation["o2"]["_id"]);

                    var obj = (BsonDocument) operation["o"];
                    var commands = obj.Names.Where(name => name.StartsWith("$")).ToArray();

                    if (commands.Any())
                    {
                        foreach (var command in commands)
                            switch (command)
                            {
                                case "$v":
                                case "$unset":
                                    break;
                                case "$set":
                                    updatedFields = (BsonDocument) obj["$set"];
                                    if (TryEmitDomainEvents(updatedFields))
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        var collection = database.GetCollection<BsonDocument>(collectionName);
                                        var document = await collection.Find(documentKey).Project(new BsonDocument("_t", 1)).FirstOrDefaultAsync();
                                        var type = EventTypeFactory.Create(document, ChangeStreamOperationType.Update, resumeToken, collectionMap[collectionName]);
                                        OnNext(updatedFields, type);
                                    }

                                    break;
                                default:
                                    throw new Exception($"Command {command} is not recognized. Timestamp: {operation["ts"]}, hash: {operation["h"]}.");
                            }
                    }
                    else
                    {
                        if (TryEmitDomainEvents(obj)) return;
                        fullDocument = obj;
                    }

                    if (fullDocument == null && updatedFields?.Any() != true) return;

                    break;
                case "d":
                    operationType = ChangeStreamOperationType.Delete;
                    documentKey = (BsonDocument) operation["o"];
                    break;
                default:
                    throw new Exception($"Unsupported operation type {operation["op"]} encountered. Timestamp: {operation["ts"]}, hash: {operation["h"]}");
            }

            OnNext(updatedFields);

            bool TryEmitDomainEvents(BsonDocument document)
            {
                if (document.TryGetValue(PrivateField.Events, out var evts) && evts is BsonArray events)
                {
                    foreach (var @event in events.Cast<BsonDocument>()) OnNext(@event, (string) @event["event"]["_t"]);
                    return true;
                }

                return false;
            }

            void OnNext(BsonDocument @event, string type = null) => observer.OnNext(new BatchItem
            {
//                Event = new Event(
//                    type ?? EventTypeFactory.Create(fullDocument, operationType, updatedFields, resumeToken, collectionMap[collectionName]),
//                    operation["ts"].AsBsonTimestamp,
//                    new EventBody {DocumentKey = documentKey, Document = fullDocument, UpdatedFields = @event}),
                Token = new BsonDocument {{"ts", operation["ts"]}, {"h", operation["h"]}},
                WallClock = (DateTime) operation["wall"]
            });
        }

        private async Task<Unit> SaveEvents(IList<BatchItem> items, ResumeToken resumeToken)
        {
            if (items.Any())
            {
                var last = items.Last();

                await events.InsertManyAsync(items.Select(item => item.Event));

                resumeToken.Token = last.Token;
                resumeToken.Updated = last.WallClock;

                await tokens.Save(resumeToken);
            }

            return Unit.Default;
        }

        private class BatchItem
        {
            public EventEnvelope Event { get; set; }
            public BsonDocument Token { get; set; }
            public DateTime WallClock { get; set; }
        }
    }
}
