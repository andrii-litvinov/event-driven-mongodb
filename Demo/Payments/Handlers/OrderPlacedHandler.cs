using System;
using System.Threading.Tasks;
using Events;
using Framework;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Payments
{
    public class OrderPlacedHandler : IEventHandler<OrderPlaced>
    {
        private readonly IMongoCollection<Payment> payments;
        public OrderPlacedHandler(IMongoDatabase database) => payments = database.GetCollection<Payment>("payments");

        public async Task Handle(OrderPlaced @event)
        {
            if (@event.Amount >= 200)
            {
                var delay = new Random().Next(3000, 30000);
                await Task.Delay(delay);
            }

            var payment = new Payment(ObjectId.GenerateNewId().ToString(), @event.SourceId);
            payment.Process(@event.Amount);
            await payments.Create(payment);
        }
    }
}