﻿using System.Threading.Tasks;
using Common;
using Common.CommandHandling;
using MongoDB.Driver;

namespace Orders
{
    public class PaymentRejectedHandler : ICommandHandler<PaymentRejected>
    {
        private readonly IMongoCollection<Order> orders;

        public PaymentRejectedHandler(IMongoDatabase database) => orders = database.GetCollection<Order>("orders");

        public async Task Handle(PaymentRejected @event)
        {
            var order = await orders.Find(o => o.Id == @event.OrderId).FirstOrDefaultAsync();
            if (order is null) return;

            order.Discard();

            await orders.Update(order);
        }
    }
}