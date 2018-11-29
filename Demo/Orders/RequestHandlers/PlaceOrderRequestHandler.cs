using System;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.CommandHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Orders
{
    public class PlaceOrderRequestHandler
    {
        private readonly IEventObservables observables;
        private readonly ICommandHandler<PlaceOrder> handler;
        private readonly IMongoCollection<Order> orders;

        public PlaceOrderRequestHandler(IEventObservables observables, ICommandHandler<PlaceOrder> handler, IMongoDatabase database)
        {
            this.observables = observables;
            this.handler = handler;
            orders = database.GetCollection<Order>("orders");
        }

        public async Task Handle(HttpRequest request, HttpResponse response, RouteData routeData)
        {
            var command = await request.ReadAs<PlaceOrder>();
            command.OrderId = ObjectId.GenerateNewId().ToString();

            // ReSharper disable once ConvertToLocalFunction
            Func<DomainEvent, bool> predicate = e => e.SourceId == command.OrderId;
            var tcs = new TaskCompletionSource<object>();

            using (observables.Observe<OrderFulfilled>(predicate).Subscribe(e => tcs.SetResult(null)))
            using (observables.Observe<OrderDiscarded>(predicate).Subscribe(e => tcs.SetResult(null)))
            {
                await handler.Handle(command);

                response.Headers.Add("Content-Type", "application/json");
                response.Headers.Add("Location", $"/orders/{command.OrderId}");

                try
                {
                    await tcs.Task.WithTimeout(TimeSpan.FromSeconds(1));
                    response.StatusCode = (int) HttpStatusCode.Created;

                    var order = await orders.Find(o => o.Id == command.OrderId).FirstAsync();
                    await response.Write(order);
                }
                catch (TimeoutException)
                {
                    response.StatusCode = (int) HttpStatusCode.Accepted;
                }
            }
        }
    }
}