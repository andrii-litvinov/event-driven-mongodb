using System.Collections.Generic;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ValueParameterNotUsed

namespace Common
{
    public abstract class Aggregate
    {
        private List<EventEnvelope> events;

        protected Aggregate(string id) => Id = id;

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