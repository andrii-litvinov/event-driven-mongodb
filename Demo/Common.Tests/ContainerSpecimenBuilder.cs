using System;
using System.Reflection;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Kernel;
using MongoDB.Bson;
using SimpleInjector;
using SubstituteAttribute = EventPublisher.Tests.SubstituteAttribute;

namespace Common.Tests
{
    internal class ContainerSpecimenBuilder : ISpecimenBuilder
    {
        private readonly Container container;
        private readonly Fixture fixture;

        public ContainerSpecimenBuilder(Container container)
        {
            this.container = container;
            container.Options.AllowOverridingRegistrations = true;

            fixture = new Fixture();
            fixture.Customize(new AutoNSubstituteCustomization {ConfigureMembers = true});
            fixture.Register(ObjectId.GenerateNewId);
        }

        public object Create(object request, ISpecimenContext context)
        {
            switch (request)
            {
                case ParameterInfo parameterInfo:
                    var serviceType = parameterInfo.ParameterType;

                    if (parameterInfo.GetCustomAttribute<SubstituteAttribute>() != null)
                    {
                        var instance = new SpecimenContext(fixture).Resolve(serviceType);
                        container.RegisterInstance(serviceType, instance);
                        return instance;
                    }

                    return Create(serviceType);
                case SeededRequest seededRequest:
                    if (seededRequest.Request is Type type) return Create(type);
                    break;
            }

            return new NoSpecimen();
        }

        private object Create(Type serviceType)
        {
            if (serviceType.IsInterface ||
                IsInterfaceArray(serviceType) ||
                IsFactory(serviceType) ||
                serviceType == typeof(Container) ||
                serviceType.Name.EndsWith("Controller"))
            {
                var producer = container.GetRegistration(serviceType);
                if (producer != null) return producer.GetInstance();
            }

            return new NoSpecimen();
        }

        private static bool IsInterfaceArray(Type serviceType) =>
            serviceType.IsArray && serviceType.GetElementType()?.IsInterface == true;

        private static bool IsFactory(Type serviceType) =>
            serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(Func<>);
    }
}