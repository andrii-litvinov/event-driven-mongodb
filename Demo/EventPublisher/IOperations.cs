using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EventPublisher
{
    public interface IOperations
    {
        Task<IAsyncCursor<BsonDocument>> GetCursor(ResumeToken resumeToken, IEnumerable<string> collections,
            CancellationToken cancellationToken);
    }
}