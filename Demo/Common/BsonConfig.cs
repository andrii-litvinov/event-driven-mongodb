using MongoDB.Bson.Serialization.Conventions;

namespace Common
{
    public static class BsonConfig
    {
        private static readonly LazyAction Lazy = new LazyAction(() =>
            ConventionRegistry.Register("conventions", new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            }, type => true));

        public static void RegisterConventionPacks() => Lazy.Invoke();
    }
}