using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Common
{
    public static class PersistenceExtensions
    {
        public static async Task Create<T>(this IMongoCollection<T> collection, T aggregate)
            where T : Aggregate =>
            await collection.InsertOneAsync(aggregate);

        public static async Task Update<T>(this IMongoCollection<T> collection, T aggregate)
            where T : Aggregate =>
            await collection.UpdateOneAsync(new ExpressionFilterDefinition<T>(a => a.Id == aggregate.Id),
                aggregate.GetUpdateDefinition());

        // TODO: Add tracing.
        private static UpdateDefinition<T> GetUpdateDefinition<T>(this T entity) =>
            Builders<T>.Update.Combine(entity.ToBsonDocument()
                .Select(element => Builders<T>.Update.Set(element.Name, element.Value)));
    }
}