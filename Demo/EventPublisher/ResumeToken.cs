using System;
using MongoDB.Bson;

namespace EventPublisher
{
    public class ResumeToken
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public BsonDocument Token { get; set; }
        public DateTime Updated { get; set; }
    }
}