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
                logEvent.AddPropertyIfAbsent(new LogEventProperty("@trace", new ScalarValue(context)));
            }
        }
    }
}