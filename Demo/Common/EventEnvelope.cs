using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Common
{
    public class EventEnvelope
    {
        public BsonDocument Event { get; set; }
        public string EventId { get; set; }
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }

        public static EventEnvelope Wrap(DomainEvent @event) => new EventEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            Event = @event.ToBsonDocument(),
            CorrelationId = TraceContext.Current.CorrelationId,
            CausationId = TraceContext.Current.CausationId
        };

        public bool TryGetDomainEvent(out DomainEvent @event)
        {
            if (BsonSerializer.Deserialize<object>(Event) is DomainEvent domainEvent)
            {
                @event = domainEvent;
                return true;
            }

            @event = null;
            return false;
        }
    }
}