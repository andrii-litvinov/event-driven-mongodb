using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Orders
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            using (var container = new Container())
            {
                BuildWebHost(container, args).Build().Run();
            }
        }

        public static IWebHostBuilder BuildWebHost(Container container, params string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder => { })
            .ConfigureServices((context, services) =>
            {
                services.AddRouting();

                container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
                services.EnableSimpleInjectorCrossWiring(container);
                services.UseSimpleInjectorAspNetRequestScoping(container);
            })
            .Configure(app =>
            {
                container.Register(typeof(ICommandHandler<>), typeof(Program).Assembly);
                container.AutoCrossWireAspNetComponents(app);
                container.Verify();

                var router = new RouteBuilder(app);

                router.MapPost("orders", async context =>
                {
                    await container.GetInstance<ICommandHandler<CreateOrder>>().Handle(await context.Request.ReadAs<CreateOrder>());
                    context.Response.StatusCode = (int) HttpStatusCode.Created;
                });

                app.UseRouter(router.Build());
            });
    }
}