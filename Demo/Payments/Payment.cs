using Common;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Payments
{
    public class Payment : Aggregate
    {
        public Payment(string id, string orderId) : base(id) => OrderId = orderId;

        public string OrderId { get; set; }
        public bool Accepted { get; set; }
        public bool Rejected { get; set; }

        public void Accept()
        {
            Accepted = true;
            RecordEvent(new PaymentAccepted(Id, OrderId));
        }

        public void Deny()
        {
            Rejected = true;
            RecordEvent(new PaymentRejected(Id, OrderId));
        }
    }
}