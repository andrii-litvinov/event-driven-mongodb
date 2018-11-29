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
        public bool Rejected { get; set; }

        public void Place(decimal totalAmount)
        {
            TotalAmount = totalAmount;
            RecordEvent(new OrderPlaced(Id, totalAmount));
        }
    }
}