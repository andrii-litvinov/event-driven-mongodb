using AutoFixture;
using AutoFixture.Xunit2;
using Common;
using Common.Tests;
using MongoDB.Bson;
using MongoDB.Driver;
using SimpleInjector;

namespace EventPublisher.Tests
{
    internal class InlineServicesAttribute : AutoDataAttribute
    {
        public InlineServicesAttribute() : base(CreateFixture)
        {
        }

        public static IFixture CreateFixture()
        {
            ConventionPacks.Register();
            ClassMaps.Register();
            
            var configuration = Configuration.GetConfiguration();
            var container = new Container();
            
            var mongoUrl = configuration["mongo:url"];
            var url = new MongoUrl(mongoUrl);
            var client = new MongoClient(url);
            var database = client.GetDatabase(url.DatabaseName);
            container.RegisterInstance(database);
            
            var fixture = new Fixture();
            fixture.Customize<ContainerSpecimenBuilder>(composer => new ContainerSpecimenBuilder(container));
            fixture.Register(ObjectId.GenerateNewId);
            return fixture;
        }
    }
}