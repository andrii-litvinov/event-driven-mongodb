using Microsoft.Extensions.Configuration;

namespace Framework
{
    public static class Configuration
    {
        public static IConfigurationRoot GetConfiguration(params string[] args) =>
            new ConfigurationBuilder().Configure(args).Build();

        public static IConfigurationBuilder Configure(this IConfigurationBuilder builder, params string[] args) =>
            builder
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args);
    }
}