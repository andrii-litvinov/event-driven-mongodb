using MongoDB.Bson.Serialization.Attributes;

namespace Framework
{
    public class Trace
    {
        public string Id { get; set; }

        [BsonIgnoreIfNull] public string CorrelationId { get; set; }

        [BsonIgnoreIfNull] public string CausationId { get; set; }
    }
}