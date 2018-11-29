using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Orders;
using Serilog;
using SimpleInjector;

namespace Payments
{
    public static class Bootstrapper
    {
        public static Container ConfigureContainer(IConfiguration configuration, ILogger logger)
        {
            ConventionPacks.Register();

            var container = new Container();

            container.RegisterInstance(configuration);
            container.RegisterInstance(logger);

            var mongoUrl = configuration["mongo:url"];
            var url = new MongoUrl(mongoUrl);
            var client = new MongoClient(url);
            var database = client.GetDatabase(url.DatabaseName);
            container.RegisterInstance(database);

            container.RegisterSingleton<IEventObservables, EventObservables>();
            container.Register(typeof(IEventHandler<>), typeof(Bootstrapper).Assembly);
            container.RegisterDecorator(typeof(IEventHandler<>), typeof(LoggerEventHandlerDecorator<>));

            container.Collection.Append(
                typeof(IHostedService),
                Lifestyle.Singleton.CreateRegistration(() => new EventHandlersConsumer("payments", database,
                    new Dictionary<string, Func<DomainEvent, Task>>
                    {
                        {nameof(OrderPlaced), @event => container.GetInstance<IEventHandler<OrderPlaced>>().Handle((OrderPlaced) @event)}
                    }, logger), container));

            return container;
        }
    }
}