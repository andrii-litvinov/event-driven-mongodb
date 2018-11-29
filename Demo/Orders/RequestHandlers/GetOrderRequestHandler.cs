using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MongoDB.Driver;

namespace Orders
{
    public class GetOrderRequestHandler
    {
        private readonly IMongoCollection<Order> orders;

        public GetOrderRequestHandler(IMongoDatabase database) => orders = database.GetCollection<Order>("orders");

        public async Task Handle(HttpRequest request, HttpResponse response, RouteData routeData)
        {
            var orderId = routeData.Values["orderId"].ToString();
            var order = await orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
            if (order is null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }
            else
            {
                response.StatusCode = (int) HttpStatusCode.Found;
                response.Headers.Add("Content-Type", "application/json");
                await response.Write(order);
            }
        }
    }
}