using System.Threading.Tasks;
using Commands;
using Common;
using Common.CommandHandling;
using MongoDB.Driver;

namespace Orders
{
    public class PlaceOrderHandler : ICommandHandler<PlaceOrder>
    {
        private readonly IMongoCollection<Order> orders;

        public PlaceOrderHandler(IMongoDatabase database) => orders = database.GetCollection<Order>("orders");

        public async Task Handle(PlaceOrder command)
        {
            var order = new Order(command.OrderId);

            order.Place(command.TotalAmount);

            await orders.Create(order);
        }
    }
}