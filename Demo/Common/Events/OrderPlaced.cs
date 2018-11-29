// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Common
{
    public class OrderPlaced : DomainEvent
    {
        public OrderPlaced(string sourceId, decimal totalAmount) : base(sourceId) => TotalAmount = totalAmount;

        public decimal TotalAmount { get; set; }
    }
}