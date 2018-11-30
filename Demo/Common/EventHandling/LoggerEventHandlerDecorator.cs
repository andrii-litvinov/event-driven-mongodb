using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;

namespace Common
{
    public class LoggerEventHandlerDecorator<TEvent> : IEventHandler<TEvent>
    {
        private readonly IEventHandler<TEvent> decorated;
        private readonly ILogger logger;

        public LoggerEventHandlerDecorator(IEventHandler<TEvent> decorated, ILogger logger)
        {
            this.decorated = decorated;
            this.logger = logger;
        }

        public async Task Handle(TEvent @event)
        {
            logger.Information("Handling {@event} event.", @event);
            var timestamp = Stopwatch.GetTimestamp();
            await decorated.Handle(@event);
            var duration = Math.Round((Stopwatch.GetTimestamp() - timestamp) / (double) TimeSpan.TicksPerMillisecond,
                4);
            logger.Information("{@event} event handled in {@duration} ms.", @event, duration);
        }
    }
}