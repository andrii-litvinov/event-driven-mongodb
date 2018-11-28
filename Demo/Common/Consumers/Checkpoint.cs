using MongoDB.Bson;

namespace Common
{
    public class Checkpoint
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public BsonTimestamp Position { get; set; }
    }
}