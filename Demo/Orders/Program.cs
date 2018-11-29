using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

                container.RegisterSingleton<IEventObservables, EventObservables>();
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
                        }, logger, container.GetInstance<IEventObservables>());
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
                    var command = await context.Request.ReadAs<PlaceOrder>();
                    command.OrderId = ObjectId.GenerateNewId().ToString();

                    var fulfilled = container.GetInstance<IEventObservables>()
                        .Subscribe<OrderFulfilled>(@event => @event.SourceId == command.OrderId)
                        .FirstAsync()
                        .ToTask();

                    var discarded = container.GetInstance<IEventObservables>()
                        .Subscribe<OrderDiscarded>(@event => @event.SourceId == command.OrderId)
                        .FirstAsync()
                        .ToTask();

                    var task = Task.WhenAny(fulfilled, discarded);

                    var handler = container.GetInstance<ICommandHandler<PlaceOrder>>();
                    await handler.Handle(command);

                    context.Response.Headers.Add("Content-Type", "application/json");
                    context.Response.Headers.Add("Location", $"/orders/{command.OrderId}");

                    try
                    {
                        var completedTask = await task.WithTimeout(TimeSpan.FromSeconds(1));
                        var @event = completedTask == fulfilled ? (DomainEvent) await fulfilled : await discarded;

                        context.Response.StatusCode = (int) HttpStatusCode.Created;
                        var serializer = new JsonSerializer {ContractResolver = new CamelCasePropertyNamesContractResolver()};
                        
                        var orders = container.GetInstance<IMongoDatabase>().GetCollection<Order>("orders");
                        var order = await orders.Find(o => o.Id == command.OrderId).FirstAsync();

                        using (var stream = new MemoryStream())
                        {
                            using (var writer = new JsonTextWriter(new StreamWriter(stream)) {Formatting = Formatting.Indented})
                            {
                                serializer.Serialize(writer, order);
                            }

                            var bytes = stream.ToArray();
                            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                        }
                    }
                    catch (TimeoutException)
                    {
                        context.Response.StatusCode = (int) HttpStatusCode.Accepted;
                    }
                });

                app.UseRouter(router.Build());
            });
    }
}