using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using Serilog;
using SimpleInjector;

namespace EventPublisher
{
    public static class Bootstrapper
    {
        public static Container ConfigureContainer(IConfiguration configuration, ILogger logger)
        {
            typeof(CollectionElementNameValidator)
                .GetField("__instance", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, new CollectionElementNameValidator36());

            var container = new Container();

            container.RegisterInstance(configuration);
            container.RegisterInstance(logger);
            container.Register<IResumeTokens, ResumeTokens>();

            var mongoUrl = configuration["mongo:url"];
            var url = new MongoUrl(mongoUrl);
            var client = new MongoClient(url);
            container.RegisterInstance(client.GetDatabase(url.DatabaseName));

            container.Collection.Append(
                typeof(IHostedService),
                Lifestyle.Singleton.CreateRegistration(() => container.CreateEventEmitter(mongoUrl), container));

            return container;
        }
    }
}