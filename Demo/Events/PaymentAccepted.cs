namespace Events
{
    public class PaymentAccepted : DomainEvent
    {
        public PaymentAccepted(string sourceId, string orderId, decimal amount) : base(sourceId)
        {
            OrderId = orderId;
            Amount = amount;
        }

        public string OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}