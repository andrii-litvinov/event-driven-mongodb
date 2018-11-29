using System;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable AccessToDisposedClosure

namespace Payments
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // TODO: Create indexes for events timestamp, type, sourceId.

            ConventionPacks.Register();
            ClassMaps.Register();

            var configuration = Configuration.GetConfiguration(args);
            using (var logger = LoggerFactory.Create(configuration))
            using (var container = Bootstrapper.ConfigureContainer(configuration, logger))
            {
                try
                {
                    logger.Information("Starting");

                    var host = new HostBuilder()
                        .ConfigureHostConfiguration(builder => builder.Configure())
                        .ConfigureServices((context, services) => services.AddSingleton(_ => container.GetAllInstances<IHostedService>()))
                        .Build();

                    container.Register(() => host.Services.GetRequiredService<IApplicationLifetime>());
                    container.Verify();

                    using (host)
                    {
                        await host.StartAsync();
                        logger.Information("Started");
                        await host.WaitForShutdownAsync();
                    }
                }
                catch (Exception e)
                {
                    logger.Fatal(e, "Event publisher crashed.");
                }

                logger.Information("Stopped");
            }
        }
    }
}