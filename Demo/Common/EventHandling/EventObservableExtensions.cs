using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Common
{
    public static class EventObservableExtensions
    {
        public static Task<DomainEvent> FirstOfType<TEvent1, TEvent2>(
            this IEventObservable observable, string sourceId)
            where TEvent1 : DomainEvent where TEvent2 : DomainEvent
        {
            var events1 = observable.Observe<TEvent1>(e => e.SourceId == sourceId).OfType<DomainEvent>();
            var events2 = observable.Observe<TEvent2>(e => e.SourceId == sourceId).OfType<DomainEvent>();
            return First(events1.Merge(events2));
        }

        private static Task<DomainEvent> First(IObservable<DomainEvent> merge)
        {
            var tcs = new TaskCompletionSource<DomainEvent>();

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