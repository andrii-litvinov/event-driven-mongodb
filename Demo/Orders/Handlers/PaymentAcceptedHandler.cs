using System.Threading.Tasks;
using Events;
using Framework;
using MongoDB.Driver;

namespace Orders
{
    public class PaymentAcceptedHandler : IEventHandler<PaymentAccepted>
    {
        private readonly IMongoCollection<Order> orders;

        public PaymentAcceptedHandler(IMongoDatabase database) => orders = database.GetCollection<Order>("orders");

        public async Task Handle(PaymentAccepted @event)
        {
            var order = await orders.Find(o => o.Id == @event.OrderId).FirstOrDefaultAsync();
            if (order is null) return;

            order.Fulfill();

            await orders.Update(order);
        }
    }
}