using AutoFixture;
using AutoFixture.Xunit2;
using Common;
using MongoDB.Bson;

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
            var container = Bootstrapper.ConfigureContainer(configuration, LoggerFactory.Create(configuration));
            var fixture = new Fixture();
            fixture.Customize<ContainerSpecimenBuilder>(composer => new ContainerSpecimenBuilder(container));
            fixture.Register(ObjectId.GenerateNewId);
            return fixture;
        }
    }
}