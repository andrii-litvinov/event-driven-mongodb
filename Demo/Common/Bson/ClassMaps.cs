using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace Common
{
    public static class ClassMaps
    {
        private static readonly LazyAction Lazy = new LazyAction(() =>
        {
            LeadAssembliesWithEvents();

            BsonClassMap.RegisterClassMap<DomainEvent>(map =>
            {
                map.AutoMap();
                map.MapMember(e => e.SourceId).SetElementName(PrivateField.SourceId).SetSerializer(new StringSerializer(BsonType.ObjectId));
            });

            BsonClassMap.RegisterClassMap<EventEnvelope>(map =>
            {
                map.AutoMap();
                map.SetDiscriminatorIsRequired(true);
            });

            var registerEvent = GetMethodInfo(RegisterEvent<object>);

            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => typeof(DomainEvent).IsAssignableFrom(type) || typeof(Aggregate).IsAssignableFrom(type))
                .Where(type => type.IsClass && !type.IsAbstract && !type.IsGenericType)
                .ToList()
                .ForEach(type => registerEvent.MakeGenericMethod(type).Invoke(null, new object[0]));
        });

        private static MethodInfo GetMethodInfo(Action action) => action.Method.GetGenericMethodDefinition();

        private static void RegisterEvent<T>() => BsonClassMap.RegisterClassMap<T>(map =>
        {
            map.AutoMap();
            map.SetDiscriminatorIsRequired(true);
        });

        private static void LeadAssembliesWithEvents()
        {
            var locations = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic).Select(assembly => assembly.Location);
            var loadedAssemblies = new HashSet<string>(locations);
            Directory
                .EnumerateFiles(Path.GetDirectoryName(typeof(ClassMaps).Assembly.Location), "*.dll")
                .Where(name => !loadedAssemblies.Contains(name))
                .ToList()
                .ForEach(name => Assembly.LoadFrom(name));
        }

        public static void Register() => Lazy.Invoke();
    }
}
