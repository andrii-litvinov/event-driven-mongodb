using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Events;

namespace Framework
{
    public class EventObservable : IEventObservable
    {
        private readonly ConcurrentDictionary<string, Subject<DomainEvent>> observables =
            new ConcurrentDictionary<string, Subject<DomainEvent>>();

        public IObservable<TEvent> Observe<TEvent>(Func<TEvent, bool> predicate) where TEvent : DomainEvent =>
            observables
                .GetOrAdd(typeof(TEvent).Name, key => new Subject<DomainEvent>())
                .OfType<TEvent>()
                .Where(predicate)
                .ObserveOn(Scheduler.Default)
                .SubscribeOn(Scheduler.Default);

        public void Publish(DomainEvent @event)
        {
            if (observables.TryGetValue(@event.GetType().Name, out var subject)) subject.OnNext(@event);
        }
    }
}