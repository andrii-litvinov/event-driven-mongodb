using System.Threading.Tasks;

namespace Orders
{
    public class CreateOrderHandler : ICommandHandler<CreateOrder>
    {
        public async Task Handle(CreateOrder command)
        {
        }
    }
}