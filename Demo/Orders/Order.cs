using Common;

namespace Orders
{
    public class Order : Aggregate
    {
        public Order(string id) : base(id)
        {
        }

        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }

        public void Place(decimal totalAmount)
        {
            TotalAmount = totalAmount;
            Status = OrderStatus.Pending;
            RecordEvent(new OrderPlaced(Id, totalAmount));
        }

        public void Fulfill()
        {
            Status = OrderStatus.Fulfilled;
            RecordEvent(new OrderFulfilled(Id));
        }

        public void Discard()
        {
            Status = OrderStatus.Discarded;
            RecordEvent(new OrderDiscarded(Id));
        }
    }
}