using System;

namespace Common
{
    public interface IEventObservables
    {
        IObservable<TEvent> Observe<TEvent>(Func<TEvent, bool> predicate) where TEvent : DomainEvent;
        void Publish(DomainEvent @event);
    }
}