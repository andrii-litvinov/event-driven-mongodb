using Common;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Payments
{
    public class Payment : Aggregate
    {
        public Payment(string id, string orderId) : base(id) => OrderId = orderId;

        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }

        public void Process(decimal amount)
        {
            Amount = amount;

            if (Amount >= 300)
            {
                Status = PaymentStatus.Rejected;
                RecordEvent(new PaymentRejected(Id, OrderId, Amount));
            }
            else
            {
                Status = PaymentStatus.Accepted;
                RecordEvent(new PaymentAccepted(Id, OrderId, Amount));
            }
        }
    }

    public enum PaymentStatus
    {
        Pending,
        Accepted,
        Rejected
    }
}