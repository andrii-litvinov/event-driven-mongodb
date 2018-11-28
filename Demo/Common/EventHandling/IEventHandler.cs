using System.Threading.Tasks;

namespace Common
{
    public interface IEventHandler<TEvent>
    {
        Task Handle(TEvent @event);
    }
}