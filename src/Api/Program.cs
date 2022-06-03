using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.log4net;
using CloutCast.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;

namespace CloutCast
{
    using Entities;
    using Modules;

    public class Program
    {
        public static readonly Assembly ThisAssembly = typeof(Program).Assembly;
        public static readonly Assembly CoreAssembly = typeof(AbstractEntity<>).Assembly;
        public static readonly Assembly LogicAssembly = Assembly.Load("CloutCast.Logic");
        public static readonly Assembly BitCloutAssembly = typeof(CoinEntry).Assembly;
        
        public static void Main(string[] args)
        {
            var builder = CreateHostBuilder(args);
            var host = builder.Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)

            .UseServiceProviderFactory(new AutofacServiceProviderFactory())

            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var jsonSources = config.Sources.OfType<JsonConfigurationSource>().ToList();
                foreach (var source in jsonSources) config.Sources.Remove(source);

                var name = hostingContext.HostingEnvironment.EnvironmentName;
                config
                    .AddNewtonsoftJsonFile("appsettings.json", true, true)
                    .AddNewtonsoftJsonFile($"appsettings.{name}.json", true, true);
            })

            .ConfigureWebHostDefaults(webBuilder => webBuilder
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>())

            .ConfigureContainer<ContainerBuilder>((context, builder) =>
            {
                builder.RegisterInstance(context.Configuration);
                builder.RegisterModule<Log4NetModule>();
                builder.RegisterModule<BitCloutModule>();
                builder.RegisterModule<MainModule>();
            });
    }
}
