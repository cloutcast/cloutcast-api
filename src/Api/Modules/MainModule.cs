using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CloutCast.Modules
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            RegisterJson(builder);
            RegisterDapper(builder);

            builder.RegisterType<SessionKeyModel>().InstancePerLifetimeScope();
            builder.RegisterType<ErrorHandlerMiddleware>();
            builder.RegisterType<ApiKeyMiddleware>();

            builder
                .Register(c => c.Resolve<IHttpClientFactory>().CreateClient())
                .As<HttpClient>()
                .InstancePerDependency();
            
            builder
                .RegisterAssemblyTypes(typeof(IFileSystem).Assembly)
                .AsImplementedInterfaces()
                .InstancePerDependency();

            builder.RegisterMediatR(Program.LogicAssembly);
            builder.RegisterMediatR(Program.BitCloutAssembly);

            builder.Populate(Enumerable.Empty<ServiceDescriptor>());
        }

        protected void RegisterDapper(ContainerBuilder builder)
        {
            builder.Register<Func<IDbConnection>>(c =>
            {
                var configuration = c.Resolve<IConfiguration>();
                var connectionString = configuration.GetConnectionString("Main");
                return () => new SqlConnection(connectionString);
            });

            var statementType = typeof(IDapperStatement);
            builder.RegisterType<DapperPipeline>().As<IDapperPipeline>().InstancePerDependency().AsSelf();
            builder
                .RegisterAssemblyTypes(Program.LogicAssembly)
                .Where(t => statementType.IsAssignableFrom(t))
                .AsImplementedInterfaces()
                .AsSelf()
                .InstancePerDependency();
        }

        protected void RegisterJson(ContainerBuilder builder)
        {
            builder
                .Register(c => ConfigureJsonSettings(new JsonSerializerSettings()))
                .As<JsonSerializerSettings>()
                .InstancePerDependency();

            builder.Register<Action<JsonSerializerSettings>>(c => s => ConfigureJsonSettings(s));
        }

        static JsonSerializerSettings ConfigureJsonSettings(JsonSerializerSettings settings)
        {
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Converters = new List<JsonConverter>
            {
                new StringEnumConverter(),
                new VersionConverter()
            };
            settings.DateParseHandling = DateParseHandling.DateTimeOffset;
            settings.DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffzzz";
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            settings.Formatting = Formatting.Indented;
            settings.NullValueHandling = NullValueHandling.Ignore;

            return settings;
        }
    }
}