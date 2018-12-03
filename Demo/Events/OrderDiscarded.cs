namespace Events
{
    public class OrderDiscarded : DomainEvent
    {
        public OrderDiscarded(string sourceId) : base(sourceId)
        {
        }
    }
}