namespace Common
{
    public class PaymentAccepted : DomainEvent
    {
        public PaymentAccepted(string sourceId, string orderId) : base(sourceId) => OrderId = orderId;
        
        public string OrderId { get; set; }
    }
}