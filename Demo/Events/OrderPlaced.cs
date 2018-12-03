// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Events
{
    public class OrderPlaced : DomainEvent
    {
        public OrderPlaced(string sourceId, decimal totalAmount) : base(sourceId) => TotalAmount = totalAmount;

        public decimal TotalAmount { get; set; }
    }
}