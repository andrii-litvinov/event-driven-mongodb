using System;
using Events;
using MongoDB.Bson;

namespace Framework
{
    public class EventEnvelope
    {
        public DomainEvent Event { get; set; }
        public BsonTimestamp Timestamp { get; set; }
        public string EventId { get; set; }
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }

        public static EventEnvelope Wrap(DomainEvent @event) => new EventEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            Event = @event,
            Timestamp = new BsonTimestamp(0, 0),
            CorrelationId = TraceContext.Current.CorrelationId,
            CausationId = TraceContext.Current.CausationId
        };
    }
}