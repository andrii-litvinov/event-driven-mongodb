using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Common
{
    public class Order
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public decimal TotalAmount { get; set; }
        public bool Paid { get; set; }

        public void Create(decimal totalAmount) => TotalAmount = totalAmount;
    }
}