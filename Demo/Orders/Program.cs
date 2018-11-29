using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.CommandHandling;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Serilog.AspNetCore;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using ILogger = Serilog.ILogger;
using LoggerFactory = Common.LoggerFactory;

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
            using (var container = new Container())
            {
                BuildWebHost(container, logger, configuration, args).Build().Run();
            }
        }

        public static IWebHostBuilder BuildWebHost(
            Container container, ILogger logger, IConfigurationRoot configuration, params string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder => { })
            .ConfigureServices((context, services) =>
            {
                services.AddRouting();

                container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
                services.EnableSimpleInjectorCrossWiring(container);
                services.UseSimpleInjectorAspNetRequestScoping(container);
                services.AddSingleton(_ => container.GetAllInstances<IHostedService>());
                services.AddSingleton<ILoggerFactory>(new SerilogLoggerFactory(logger));

                container.RegisterInstance(logger);
                container.Register(typeof(ICommandHandler<>), typeof(Program).Assembly);
                container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggerCommandHandlerDecorator<>));

                var mongoUrl = configuration["mongo:url"];
                var url = new MongoUrl(mongoUrl);
                var client = new MongoClient(url);
                var database = client.GetDatabase(url.DatabaseName);
                container.RegisterInstance(database);
                
                container.Register(typeof(IEventHandler<>), typeof(Program).Assembly);
                container.RegisterDecorator(typeof(IEventHandler<>), typeof(LoggerEventHandlerDecorator<>));

                container.Collection.Append(
                    typeof(IHostedService),
                    Lifestyle.Singleton.CreateRegistration(() =>
                    {
                        var consumer = new EventConsumer("orders", database, new Dictionary<string, Func<DomainEvent, Task>>
                        {
                            {
                                nameof(PaymentAccepted),
                                @event => container.GetInstance<IEventHandler<PaymentAccepted>>().Handle((PaymentAccepted) @event)
                            },
                            {
                                nameof(PaymentRejected),
                                @event => container.GetInstance<IEventHandler<PaymentRejected>>().Handle((PaymentRejected) @event)
                            }
                        }, logger);
                        return consumer;
                    }, container));
            })
            .Configure(app =>
            {
                container.AutoCrossWireAspNetComponents(app);
                container.Verify();

                var router = new RouteBuilder(app);

                router.MapPost("orders", async context =>
                {
                    await container.GetInstance<ICommandHandler<PlaceOrder>>().Handle(await context.Request.ReadAs<PlaceOrder>());
                    context.Response.StatusCode = (int) HttpStatusCode.Created;
                });

                app.UseRouter(router.Build());
            });
    }
}