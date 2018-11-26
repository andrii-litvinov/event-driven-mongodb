using System;
using System.Threading;

namespace Domain
{
    public class EventEnvelope
    {
        public DomainEvent Event { get; set; }
        public string EventId { get; set; }
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }

        public static EventEnvelope Wrap(DomainEvent @event) => new EventEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            Event = @event,
            CorrelationId = TraceContext.Current.CorrelationId,
            CausationId = TraceContext.Current.CausationId
        };
    }
    
    public abstract class DomainEvent
    {
        protected DomainEvent(string sourceId) => SourceId = sourceId;

        public string SourceId { get; set; }
    }
    
    public class TraceContext
    {
        public static readonly TraceContext Empty = new TraceContext();
        private static readonly AsyncLocal<TraceContext> context = new AsyncLocal<TraceContext>();

        public static TraceContext Current => context.Value ?? Empty;

        public string CorrelationId { get; private set; }
        public string CausationId { get; private set; }

        public static void Set(string correlationId, string causationId) => context.Value = new TraceContext
        {
            CorrelationId = correlationId,
            CausationId = causationId
        };
    }
}
