using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using SimpleInjector;

namespace EventPublisher
{
    public static class EventEmitterFactory
    {
        public static EventEmitterService CreateEventEmitter(this Container container, string name, string mongoUrl,
            IEnumerable<string> collectionNames)
        {
            var url = new MongoUrl(mongoUrl);
            var client = new MongoClient(url);
            var collection = client.GetDatabase("local").GetCollection<BsonDocument>("oplog.rs");
            var operations = new Operations(collection, url.DatabaseName);

            return new EventEmitterService(
                name,
                container.GetInstance<IMongoDatabase>(),
                operations,
                container.GetInstance<IResumeTokens>(),
                container.GetInstance<ILogger>(),
                collectionNames);
        }

        public static EventEmitterService CreateEventEmitter(this Container container, string mongoUrl) =>
            container.CreateEventEmitter("emitter", mongoUrl, new[] {"orders", "payments"});
    }
}