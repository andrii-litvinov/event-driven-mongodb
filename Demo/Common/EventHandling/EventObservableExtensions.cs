using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Common
{
    public static class EventObservableExtensions
    {
        public static Task<TEvent1> FirstOfType<TEvent1>(
            this IEventObservable observable, string sourceId)
            where TEvent1 : DomainEvent =>
            First(observable.Observe<TEvent1>(e => e.SourceId == sourceId));

        public static Task<DomainEvent> FirstOfType<TEvent1, TEvent2>(
            this IEventObservable observable, string sourceId)
            where TEvent1 : DomainEvent where TEvent2 : DomainEvent
        {
            var events1 = observable.Observe<TEvent1>(e => e.SourceId == sourceId).OfType<DomainEvent>();
            var events2 = observable.Observe<TEvent2>(e => e.SourceId == sourceId).OfType<DomainEvent>();
            return First(events1.Merge(events2));
        }

        public static Task<DomainEvent> FirstOfType<TEvent1, TEvent2, TEvent3>(
            this IEventObservable observable, string sourceId)
            where TEvent1 : DomainEvent where TEvent2 : DomainEvent where TEvent3 : DomainEvent
        {
            var events1 = observable.Observe<TEvent1>(e => e.SourceId == sourceId).OfType<DomainEvent>();
            var events2 = observable.Observe<TEvent2>(e => e.SourceId == sourceId).OfType<DomainEvent>();
            var events3 = observable.Observe<TEvent3>(e => e.SourceId == sourceId).OfType<DomainEvent>();
            return First(events1.Merge(events2).Merge(events3));
        }

        private static Task<TEvent> First<TEvent>(IObservable<TEvent> merge)
        {
            var tcs = new TaskCompletionSource<TEvent>();

            var subscription = merge
                .ObserveOn(Scheduler.Default)
                .SubscribeOn(Scheduler.Default)
                .Subscribe(e => tcs.TrySetResult(e));

            var task = tcs.Task.WithTimeout(TimeSpan.FromSeconds(1));
            task.ContinueWith(_ => subscription.Dispose());

            return task;
        }
    }
}