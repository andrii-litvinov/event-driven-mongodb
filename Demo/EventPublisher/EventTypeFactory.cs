using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EventPublisher
{
    public static class EventTypeFactory
    {
        public static string Create(BsonDocument document, ChangeStreamOperationType operationType,
            ResumeToken resumeToken, string defaultEventPrefix)
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
                case ChangeStreamOperationType.Invalidate:
                    throw new Exception(
                        $"Collection {resumeToken.Name} was invalidated. Consider what to do in such cases and deploy new version of app with correcting actions.");
                default:
                    throw new Exception($"Unknown operation type {operationType} for collection {resumeToken.Name}.");
            }
        }
    }
}