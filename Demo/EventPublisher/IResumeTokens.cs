using System.Threading;
using System.Threading.Tasks;

namespace EventPublisher
{
    public interface IResumeTokens
    {
        Task<ResumeToken> Get(string name, CancellationToken cancellationToken = default);
        Task Save(ResumeToken resumeToken);
        Task RemoveAll(string name);
    }
}