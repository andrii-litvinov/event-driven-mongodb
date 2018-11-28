using System.Threading.Tasks;
using Common;

namespace Payments
{
    public class OrderCreatedEventHandler : IEventHandler<OrderCreated>
    {
        public async Task Handle(OrderCreated @event)
        {
        }
    }
}