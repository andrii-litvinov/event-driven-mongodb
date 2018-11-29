using Common;

namespace Orders
{
    public class Order : Aggregate
    {
        public Order(string id) : base(id)
        {
        }

        public decimal TotalAmount { get; set; }
        public bool Fulfilled { get; set; }
        public bool Discarded { get; set; }

        public void Place(decimal totalAmount)
        {
            TotalAmount = totalAmount;
            RecordEvent(new OrderPlaced(Id, totalAmount));
        }

        public void Fulfill()
        {
            Fulfilled = true;
            RecordEvent(new OrderFulfilled(Id));
        }

        public void Discard()
        {
            Discarded = true;
            RecordEvent(new OrderDiscarded(Id));
        }
    }
}