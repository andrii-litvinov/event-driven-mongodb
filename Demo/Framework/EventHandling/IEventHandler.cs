using System.Threading.Tasks;

namespace Framework
{
    public interface IEventHandler<TEvent>
    {
        Task Handle(TEvent @event);
    }
}