

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Events
{
    public class NumberMultiplied : DomainEvent
    {
        public NumberMultiplied(string id, decimal number, decimal result) : base(id)
        {
            Number = number;
            Result = result;
        }

        public decimal Number { get; set; }
        public decimal Result { get; set; }
    }
}