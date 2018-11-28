using MongoDB.Bson;

namespace Common
{
    public class Order
    {
        public ObjectId Id { get; set; }
        public decimal TotalAmount { get; set; }
        public bool Paid { get; set; }

        public void Create(decimal totalAmount) => TotalAmount = totalAmount;
    }
}