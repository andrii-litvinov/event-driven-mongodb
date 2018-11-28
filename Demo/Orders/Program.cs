using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Orders
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Build().Run();
        }

        public static IWebHostBuilder BuildWebHost(params string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder => { })
            .ConfigureServices((context, services) =>
            {
                services.AddRouting();
                services.AddScoped<CreateOrderHandler>();
            })
            .Configure(app =>
            {
                var router = new RouteBuilder(app);

                router.MapPost("orders", async context =>
                {
                    await context.RequestServices.GetRequiredService<CreateOrderHandler>().Handle(await context.Request.ReadAs<CreateOrder>());
                    context.Response.StatusCode = (int) HttpStatusCode.Created;
                });

                app.UseRouter(router.Build());
            });
    }
}