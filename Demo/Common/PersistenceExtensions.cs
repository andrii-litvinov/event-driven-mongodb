using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Common
{
    public static class PersistenceExtensions
    {
        public static async Task Create<T>(this IMongoCollection<T> collection, T aggregate)
            where T : Aggregate
        {
            await collection.InsertOneAsync(aggregate);
            aggregate.Events?.Clear();
        }

        public static async Task Update<T>(this IMongoCollection<T> collection, T aggregate)
            where T : Aggregate
        {
            await collection.UpdateOneAsync(a => a.Id == aggregate.Id, aggregate.GetUpdateDefinition());
            aggregate.Events?.Clear();
        }

        public static async Task Replace<T>(this IMongoCollection<T> collection, T aggregate)
            where T : Aggregate
        {
            await collection.ReplaceOneAsync(a => a.Id == aggregate.Id, aggregate);
            aggregate.Events?.Clear();
        }

        private static UpdateDefinition<T> GetUpdateDefinition<T>(this T entity) =>
            Builders<T>.Update.Combine(entity.ToBsonDocument()
                .Select(element => Builders<T>.Update.Set(element.Name, element.Value)));
    }
}