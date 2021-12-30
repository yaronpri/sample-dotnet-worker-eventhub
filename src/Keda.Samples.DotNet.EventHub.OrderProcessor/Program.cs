using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Keda.Samples.DotNet.EventHub.OrderProcessor
{
    public class Program
    {
        public static void Main(string[] args)
        {            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostBuilderContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConsole(consoleLoggerOptions => consoleLoggerOptions.TimestampFormat = "[HH:mm:ss]");
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<OrdersQueueProcessor>();
                });
    }
}
