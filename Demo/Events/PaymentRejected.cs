// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Events
{
    public class PaymentRejected : DomainEvent
    {
        public PaymentRejected(string id, string orderId, decimal amount) : base(id)
        {
            OrderId = orderId;
            Amount = amount;
        }

        public string OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}