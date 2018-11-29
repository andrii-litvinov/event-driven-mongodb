using System.Threading.Tasks;

namespace EventPublisher
{
    public interface IStartupOperation
    {
        Task Execute();
    }
}