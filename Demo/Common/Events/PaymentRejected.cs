// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Common
{
    public class PaymentRejected : DomainEvent
    {
        public PaymentRejected(string id, string orderId) : base(id) => OrderId = orderId;

        public string OrderId { get; set; }
    }
}