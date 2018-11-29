using System.Threading.Tasks;

namespace Common.CommandHandling
{
    public interface ICommandHandler<TCommand>
    {
        Task Handle(TCommand command);
    }
}