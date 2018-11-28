using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Orders
{
    public class CreateOrderHandler : ICommandHandler<CreateOrder>
    {
        private readonly IMongoCollection<Order> orders;

        public CreateOrderHandler(IMongoDatabase database) => orders = database.GetCollection<Order>("orders");

        public async Task Handle(CreateOrder command)
        {
            var order = new Order();

            // Whatever logic to create an order form a command
            order.Create(command.TotalAmount);

            await orders.InsertOneAsync(order);
        }
    }

    public class Order
    {
        public ObjectId Id { get; set; }
        public decimal TotalAmount { get; set; }

        public void Create(decimal totalAmount) => TotalAmount = totalAmount;
    }
}