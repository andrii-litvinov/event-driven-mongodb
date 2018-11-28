using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;

namespace Orders
{
    public class LoggerDecorator<TCommand> : ICommandHandler<TCommand>
    {
        private readonly ICommandHandler<TCommand> decorated;
        private readonly ILogger logger;

        public LoggerDecorator(ICommandHandler<TCommand> decorated, ILogger logger)
        {
            this.decorated = decorated;
            this.logger = logger;
        }

        public async Task Handle(TCommand command)
        {
            logger.Information("Handling {@command} command.", command);
            var timestamp = Stopwatch.GetTimestamp();
            await decorated.Handle(command);
            var duration = Math.Round((Stopwatch.GetTimestamp() - timestamp) / (double) TimeSpan.TicksPerMillisecond, 4);
            logger.Information("{@command} command handled in {@duration}ms.", command, duration);
        }
    }
}