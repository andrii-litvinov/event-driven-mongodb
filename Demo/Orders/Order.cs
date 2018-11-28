using MongoDB.Bson;

namespace Orders
{
    public class Order
    {
        public ObjectId Id { get; set; }
        public decimal TotalAmount { get; set; }

        public void Create(decimal totalAmount) => TotalAmount = totalAmount;
    }
}