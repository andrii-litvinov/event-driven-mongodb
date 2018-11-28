using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Polly;
using Serilog;

namespace Common
{
    public abstract class ResilientService : BackgroundService
    {
        private readonly ILogger logger;

        protected ResilientService(ILogger logger) => this.logger = logger;

        public sealed override Task StartAsync(CancellationToken cancellationToken) => base.StartAsync(cancellationToken);
        public sealed override Task StopAsync(CancellationToken cancellationToken) => base.StopAsync(cancellationToken);

        protected abstract Task Execute(CancellationToken cancellationToken);

        protected sealed override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var retryForever = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    i => TimeSpan.FromSeconds(30),
                    (e, _) => logger.Fatal(e, "Service faulted. Restarting."));

            var stopOnCancellation = Policy
                .Handle<OperationCanceledException>()
                .FallbackAsync(async ct => { });

            await Policy
                .WrapAsync(retryForever, stopOnCancellation)
                .ExecuteAsync(Execute, cancellationToken);
        }
    }
}
