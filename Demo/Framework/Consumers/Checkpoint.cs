using MongoDB.Bson;

namespace Framework
{
    public class Checkpoint
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public BsonTimestamp Position { get; set; }
    }
}