using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Common
{
    public class Order : Aggregate
    {
        public decimal TotalAmount { get; set; }
        public bool Paid { get; set; }

        public void Create(decimal totalAmount) => TotalAmount = totalAmount;
    }
}