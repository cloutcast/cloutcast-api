using System;
using System.Linq;
using Alba;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.log4net;
using Dapper;
using FluentMigrator.Runner;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CloutCast
{
    using Contracts;
    using Migrations;
    using Modules;

    public partial class WebAppFixture : IDisposable
    {
        private ServiceProvider _serviceProvider;
        const string TestApiKey = "82e4a9f3-092a-45d9-9d89-55fe86fc9a25";

        public readonly SystemUnderTest SystemUnderTest;
        protected static string ConnString() => "server=localhost;database=cc;Integrated Security=true";

        private IHostBuilder TestHost() => Host
            .CreateDefaultBuilder()

            .UseServiceProviderFactory(new AutofacServiceProviderFactory())

            .UseEnvironment("IntegrationTests")

            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var jsonSources = config.Sources.OfType<JsonConfigurationSource>().ToList();
                foreach (var source in jsonSources) config.Sources.Remove(source);

                config.AddNewtonsoftJsonFile("appsettings.integration.json", true, true);
            })

            .ConfigureWebHostDefaults(webBuilder => webBuilder
                .UseSolutionRelativeContentRoot(@".\src\Api\")
                .UseStartup<TestStartup>())

            .ConfigureServices(services =>
            {
                //http://parsstudent.com/fluentmigrator-aspnet-core-2/
                services
                    .AddFluentMigratorCore()
                    .AddLogging(lb => lb.AddFluentMigratorConsole())
                    .Configure<FluentMigratorLoggerOptions>(options =>
                    {
                        options.ShowSql = true;
                        options.ShowElapsedTime = true;
                    })
                    .ConfigureRunner(rb => rb
                        .AddSqlServer2016()
                        .WithGlobalConnectionString(ConnString())
                        .ScanIn(typeof(Migration001_Create_Table_BitCloutUser).Assembly).For.All()
                    );
                _serviceProvider = services.BuildServiceProvider();
            })
            
            .ConfigureContainer<ContainerBuilder>((context, builder) =>
            {
                builder.RegisterInstance(context.Configuration);
                builder.RegisterModule<Log4NetModule>();
                builder.RegisterModule<BitCloutModule>();
                builder.RegisterModule<MainModule>();
            });

        public WebAppFixture()
        {
            SystemUnderTest = new SystemUnderTest(TestHost());
            SystemUnderTest.BeforeEach(httpContext =>
            {
                httpContext.Request.Headers["x-api-key"] = TestApiKey;
            });

            Database.Reset();

            // Run the migrations
            var runner = Resolve<IMigrationRunner>();
            runner.MigrateUp();

            Database.UseConnection(c => c.Execute($"update {Tables.App} set ApiKey = '{TestApiKey}' where Name = 'CloutCast'"));
        }

        public T Resolve<T>() => (T) SystemUnderTest.Services.GetService(typeof(T));

        public WebAppFixture SetActiveUser(IBitCloutUser user)
        {
            LocalAuthenticationHandler.User = user;
            return this;
        }

        public V Mediator<R, V>(Action<R> setup) where R: IRequest<V>, new()
        {
            var request = new R();
            setup(request);
            var mediator = Resolve<IMediator>();
            var task = mediator.Send(request);
            task.Wait();

            return task.Result;
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
            SystemUnderTest?.Dispose();
        }
    }

    [CollectionDefinition("CloutCast Fixture")]
    public class DatabaseCollection : ICollectionFixture<WebAppFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}