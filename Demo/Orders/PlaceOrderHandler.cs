using System.Threading.Tasks;
using Common;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Orders
{
    public class PlaceOrderHandler : ICommandHandler<PlaceOrder>
    {
        private readonly IMongoCollection<Order> orders;

        public PlaceOrderHandler(IMongoDatabase database) => orders = database.GetCollection<Order>("orders");

        public async Task Handle(PlaceOrder command)
        {
            var order = new Order(ObjectId.GenerateNewId().ToString());

            order.Place(command.TotalAmount);

            await orders.InsertOneAsync(order);
        }
    }
}