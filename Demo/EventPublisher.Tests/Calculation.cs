using Common;
using Events;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EventPublisher.Tests
{
    public class Calculation : Aggregate
    {
        public Calculation(string id) : base(id)
        {
        }

        public decimal Result { get; set; }

        public void Add(decimal number)
        {
            Result += number;
            RecordEvent(new NumberAdded(Id, number, Result));
        }

        public void MultiplyBy(decimal number)
        {
            Result *= number;
            RecordEvent(new NumberMultiplied(Id, number, Result));
        }
    }
}