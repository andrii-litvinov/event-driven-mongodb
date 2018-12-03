using Serilog.Core;
using Serilog.Events;

namespace Common
{
    public class TraceContextEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var context = TraceContext.Current;
            if (context != TraceContext.Empty)
            {
                if (!string.IsNullOrEmpty(context.CorrelationId))
                    logEvent.AddPropertyIfAbsent(new LogEventProperty(nameof(context.CorrelationId), new ScalarValue(context.CorrelationId)));
                
                if (!string.IsNullOrEmpty(context.CausationId))
                    logEvent.AddPropertyIfAbsent(new LogEventProperty(nameof(context.CausationId), new ScalarValue(context.CausationId)));
            }
        }
    }
}