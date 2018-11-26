using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EventPublisher
{
    public static class EventTypeFactory
    {
        public static string Create(BsonDocument document, ChangeStreamOperationType operationType, string defaultEventPrefix)
        {
            var prefix = defaultEventPrefix;
            if (document != null && document.TryGetValue("_t", out var t)) prefix = (string) t;

            switch (operationType)
            {
                case ChangeStreamOperationType.Insert:
                    return $"{prefix}Created";
                case ChangeStreamOperationType.Update:
                    return $"{prefix}Updated";
                case ChangeStreamOperationType.Replace:
                    return $"{prefix}Updated";
                case ChangeStreamOperationType.Delete:
                    return $"{prefix}Deleted";
                default:
                    throw new Exception($"Unsupported operation type {operationType}.");
            }
        }
    }
}