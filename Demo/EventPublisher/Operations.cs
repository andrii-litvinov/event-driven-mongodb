using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EventPublisher
{
    public class Operations : IOperations
    {
        private readonly FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
        private readonly string databaseName;
        private readonly IMongoCollection<BsonDocument> operations;

        public Operations(IMongoCollection<BsonDocument> operations, string databaseName)
        {
            this.operations = operations;
            this.databaseName = databaseName;
        }

        public async Task<IAsyncCursor<BsonDocument>> GetCursor(ResumeToken resumeToken, IEnumerable<string> collections, CancellationToken cancellationToken)
        {
            BsonValue ts;

            if (resumeToken.Token != null)
            {
                ts = resumeToken.Token["ts"];
            }
            else
            {
                var operation = await operations
                    .Find(new BsonDocument())
                    .Sort(Builders<BsonDocument>.Sort.Descending("$natural"))
                    .FirstAsync(cancellationToken);
                ts = operation["ts"];
            }

            var filter = builder.And(
                builder.In("op", new[] {"i", "u", "d"}),
                builder.Gt("ts", ts),
                builder.In("ns", collections.Select(collectionName => $"{databaseName}.{collectionName}")),
                builder.Exists("fromMigrate", false)
            );

            var options = new FindOptions<BsonDocument> {CursorType = CursorType.TailableAwait, NoCursorTimeout = true, OplogReplay = true};
            return await operations.FindAsync(filter, options, cancellationToken);
        }
    }
}
