namespace Domain
{
    public abstract class DomainEvent
    {
        protected DomainEvent(string sourceId) => SourceId = sourceId;

        public string SourceId { get; set; }
    }
}