using Events;
using Framework;

namespace Orders
{
    public class Order : Aggregate
    {
        public Order(string id) : base(id)
        {
        }

        public decimal Amount { get; set; }
        public OrderStatus Status { get; set; }

        public void Place(decimal amount)
        {
            Amount = amount;
            Status = OrderStatus.Pending;
            RecordEvent(new OrderPlaced(Id, amount));
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