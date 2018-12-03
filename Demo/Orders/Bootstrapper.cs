using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Events;
using Framework;
using Framework.CommandHandling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Serilog;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Orders
{
    public static class Bootstrapper
    {
        public static Container ConfigureContainer(IConfiguration configuration, ILogger logger)
        {
            var container = new Container();

            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
            container.RegisterInstance(logger);
            container.Register(typeof(ICommandHandler<>), typeof(Program).Assembly);
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggerCommandHandlerDecorator<>));

            var mongoUrl = configuration["mongo:url"];
            var url = new MongoUrl(mongoUrl);
            var client = new MongoClient(url);
            var database = client.GetDatabase(url.DatabaseName);
            container.RegisterInstance(database);

            container.RegisterSingleton<IEventObservable, EventObservable>();
            container.Register(typeof(IEventHandler<>), typeof(Program).Assembly);
            container.RegisterDecorator(typeof(IEventHandler<>), typeof(LoggerEventHandlerDecorator<>));

            container.Collection.Append(
                typeof(IHostedService),
                Lifestyle.Singleton.CreateRegistration(() => new EventHandlersConsumer("orders", database,
                    new Dictionary<string, Func<DomainEvent, Task>>
                    {
                        {
                            nameof(PaymentAccepted),
                            @event => container.GetInstance<IEventHandler<PaymentAccepted>>()
                                .Handle((PaymentAccepted) @event)
                        },
                        {
                            nameof(PaymentRejected),
                            @event => container.GetInstance<IEventHandler<PaymentRejected>>()
                                .Handle((PaymentRejected) @event)
                        }
                    }, logger), container));

            container.Collection.Append(
                typeof(IHostedService),
                Lifestyle.Singleton.CreateRegistration(
                    () => new EventObserverConsumer("orders-observers", database, logger,
                        container.GetInstance<IEventObservable>()),
                    container));

            return container;
        }
    }
}