namespace Common
{
    public class OrderCreated : DomainEvent
    {
        public OrderCreated(string sourceId) : base(sourceId)
        {
        }

        public decimal TotalAmount { get; set; }
    }
}