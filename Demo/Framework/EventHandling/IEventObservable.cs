using System;
using Events;

namespace Framework
{
    public interface IEventObservable
    {
        IObservable<TEvent> Observe<TEvent>(Func<TEvent, bool> predicate) where TEvent : DomainEvent;
        void Publish(DomainEvent @event);
    }
}