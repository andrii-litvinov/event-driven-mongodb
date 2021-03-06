﻿using System.Threading.Tasks;
using Commands;
using Framework;
using Framework.CommandHandling;
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

            order.Place(command.Amount);

            await orders.Create(order);
        }
    }
}