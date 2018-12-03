using System.Threading.Tasks;

namespace Framework.CommandHandling
{
    public interface ICommandHandler<TCommand>
    {
        Task Handle(TCommand command);
    }
}