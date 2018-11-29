namespace Common.Tests
{
    public class AggregateModified : DomainEvent
    {
        public AggregateModified(string sourceId) : base(sourceId)
        {
        }
    }
}