using Common;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EventPublisher.Tests
{
    public class NumberAdded : DomainEvent
    {
        public NumberAdded(string id, decimal number, decimal result) : base(id)
        {
            Number = number;
            Result = result;
        }

        public decimal Number { get; set; }
        public decimal Result { get; set; }
    }
}