using System.Threading;

namespace Common
{
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