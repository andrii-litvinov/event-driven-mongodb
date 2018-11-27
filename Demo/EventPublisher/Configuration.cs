using Microsoft.Extensions.Configuration;

namespace EventPublisher
{
    public static class Configuration
    {
        public static IConfigurationRoot GetConfiguration(params string[] args) => new ConfigurationBuilder().Configure(args).Build();

        public static IConfigurationBuilder Configure(this IConfigurationBuilder builder, params string[] args) => builder
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables("synergy:")
            .AddCommandLine(args);
    }
}
