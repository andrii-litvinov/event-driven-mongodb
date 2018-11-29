using MongoDB.Bson.Serialization.Conventions;

namespace Common
{
    public static class ConventionPacks
    {
        private static readonly LazyAction Lazy = new LazyAction(() =>
            ConventionRegistry.Register("conventions", new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            }, type => true));

        public static void Register() => Lazy.Invoke();
    }
}