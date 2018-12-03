using System;
using System.Net;
using System.Threading.Tasks;
using Commands;
using Common;
using Common.CommandHandling;
using Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Orders
{
    public class PlaceOrderRequestHandler
    {
        private readonly ICommandHandler<PlaceOrder> handler;
        private readonly IEventObservable observable;
        private readonly IMongoCollection<Order> orders;

        public PlaceOrderRequestHandler(IEventObservable observable, ICommandHandler<PlaceOrder> handler,
            IMongoDatabase database)
        {
            this.observable = observable;
            this.handler = handler;
            orders = database.GetCollection<Order>("orders");
        }

        public async Task Handle(HttpRequest request, HttpResponse response, RouteData routeData)
        {
            var orderId = ObjectId.GenerateNewId().ToString();

            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("Location", $"{request.Scheme}://{request.Host}{request.Path}/{orderId}");

            var futureEvent = observable.FirstOfType<OrderFulfilled, OrderDiscarded>(orderId);

            var command = await request.Body.ReadAs<PlaceOrder>();
            command.OrderId = orderId;

            await handler.Handle(command);

            try
            {
                await futureEvent;
                response.StatusCode = (int) HttpStatusCode.Created;
            }
            catch (TimeoutException)
            {
                response.StatusCode = (int) HttpStatusCode.Accepted;
            }

            await response.Body.Write(await orders.Find(o => o.Id == orderId).FirstAsync());
        }
    }
}