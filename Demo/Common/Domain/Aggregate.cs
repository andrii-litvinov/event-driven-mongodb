using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable ValueParameterNotUsed

namespace Common
{
    public abstract class Aggregate
    {
        private List<EventEnvelope> events;

        public string Id { get; set; }

        public List<EventEnvelope> Events
        {
            get => events;
            set { }
        }

        public void RecordEvent(DomainEvent @event)
        {
            if (events is null) events = new List<EventEnvelope>();
            events.Add(EventEnvelope.Wrap(@event));
        }
    }
}