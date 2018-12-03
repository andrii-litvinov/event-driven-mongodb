// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Events
{
    public class OrderPlaced : DomainEvent
    {
        public OrderPlaced(string sourceId, decimal amount) : base(sourceId) => Amount = amount;

        public decimal Amount { get; set; }
    }
}