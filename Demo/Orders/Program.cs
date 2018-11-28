using System.Net;
using Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Serilog;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Orders
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            BsonConfig.RegisterConventionPacks();
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

                container.RegisterInstance(logger);
                container.Register(typeof(ICommandHandler<>), typeof(Program).Assembly);
                container.Collection.Register<IHostedService>(typeof(Program).Assembly);
                
                var mongoUrl = configuration["mongo:url"];
                var url = new MongoUrl(mongoUrl);
                var client = new MongoClient(url);
                var database = client.GetDatabase(url.DatabaseName);
                container.RegisterInstance(database);
            })
            .Configure(app =>
            {
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