using System.Threading.Tasks;
using Common;

namespace Payments
{
    public class OrderPlacedHandler : IEventHandler<OrderPlaced>
    {
        public async Task Handle(OrderPlaced @event)
        {
        }
    }
}