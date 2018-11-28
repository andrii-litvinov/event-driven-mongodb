using System.Threading.Tasks;

namespace Orders
{
    public interface ICommandHandler<TCommand>
    {
        Task Handle(TCommand command);
    }
}