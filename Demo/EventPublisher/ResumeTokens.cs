using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace EventPublisher
{
    public class ResumeTokens : IResumeTokens
    {
        private readonly IMongoCollection<ResumeToken> tokens;

        public ResumeTokens(IMongoDatabase database) => tokens = tokens = database.GetCollection<ResumeToken>("resumetokens");

        public async Task<ResumeToken> Get(string name, CancellationToken cancellationToken = default) =>
            await tokens.Find(t => t.Name == name).FirstOrDefaultAsync(cancellationToken) ??
            new ResumeToken {Name = name, Updated = DateTime.UtcNow};

        public async Task Save(ResumeToken resumeToken)
        {
            if (resumeToken.Id == default)
                await tokens.InsertOneAsync(resumeToken);
            else
                await tokens.UpdateOneAsync(token => token.Id == resumeToken.Id,
                    Builders<ResumeToken>.Update.Combine(
                        Builders<ResumeToken>.Update.Set(token => token.Token, resumeToken.Token),
                        Builders<ResumeToken>.Update.Set(token => token.Updated, resumeToken.Updated)));
        }

        public async Task RemoveAll(string name) => await tokens.DeleteManyAsync(token => token.Name == name);
    }
}