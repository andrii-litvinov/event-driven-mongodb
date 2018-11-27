using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace EventPublisher
{
    public static class LoggerFactory
    {
        public static Logger Create(IConfiguration configuration)
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName();
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Async(c => c.LiterateConsole())
                .Enrich.WithDemystifiedStackTraces()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("OsVersion", Environment.OSVersion)
                .Enrich.WithProperty("Version", assemblyName.Version)
                .Enrich.WithProperty("OsUser", Environment.UserName)
                .Enrich.WithProperty("Application", $"{assemblyName.Name}.{configuration["environment"]}")
                .Enrich.WithProperty("Executable", assemblyName.Name)
                .Enrich.WithProperty("Environment", configuration["environment"]);

            return loggerConfiguration.CreateLogger();
        }
    }
}
