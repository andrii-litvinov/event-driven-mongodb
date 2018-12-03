using System;
using Framework;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.AspNetCore;
using SimpleInjector;
using ILogger = Serilog.ILogger;
using LoggerFactory = Framework.LoggerFactory;

namespace Orders
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            ConventionPacks.Register();
            ClassMaps.Register();

            var configuration = Configuration.GetConfiguration(args);
            using (var logger = LoggerFactory.Create(configuration))
            using (var container = Bootstrapper.ConfigureContainer(configuration, logger))
            {
                BuildWebHost(container, logger, configuration, args).Build().Run();
            }
        }

        private static IWebHostBuilder BuildWebHost(
            Container container, ILogger logger, IConfiguration configuration, params string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder => { })
            .ConfigureServices((context, services) =>
            {
                services.AddRouting();

                services.EnableSimpleInjectorCrossWiring(container);
                services.UseSimpleInjectorAspNetRequestScoping(container);
                services.AddSingleton(_ => container.GetAllInstances<IHostedService>());
                services.AddSingleton<ILoggerFactory>(new SerilogLoggerFactory(logger));
            })
            .Configure(app =>
            {
                container.AutoCrossWireAspNetComponents(app);
                container.Verify();

                app.Use(async (context, next) =>
                {
                    TraceContext.Set(Guid.NewGuid().ToString(), null);
                    await next.Invoke();
                });

                var router = new RouteBuilder(app);

                router.MapPost("orders", container.GetInstance<PlaceOrderRequestHandler>().Handle);
                router.MapGet("orders/{orderId}", container.GetInstance<GetOrderRequestHandler>().Handle);

                app.UseRouter(router.Build());
            });
    }
}