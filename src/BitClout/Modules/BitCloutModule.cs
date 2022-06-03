using System;
using System.Collections.Generic;
using Autofac;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace CloutCast.Modules
{
    using Options;

    public class BitCloutModule : Module
    {
        /// <summary>
        ///    Adds RestSharp.Injection-based registrations to the container.
        /// </summary>
        /// <param name="builder">
        ///    The builder through which components can be registered.
        /// </param>
        /// <remarks>
        ///    Note that the ContainerBuilder parameter is unique to this module.
        /// </remarks>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterMediatR(ThisAssembly);
            
            builder.RegisterInstance<Func<IRestClient>>(() => new RestClient
            {
                Timeout = 60000, // 10 min timeout,
                ReadWriteTimeout = 60000,
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.128 Safari/537.36 Edg/89.0.774.77"
            });
            
            builder.Register(ctx =>
            {
                var configuration = ctx.Resolve<IConfiguration>();
                return configuration.GetSection("BitClout").Get<BitCloutOption>();

            }).SingleInstance();
            builder
                .RegisterType<BitCloutRestFactory>()
                .AsImplementedInterfaces()
                .AsSelf();

            builder.RegisterInstance<Func<string, Method, IRestRequest>>(
                (resource, method) => new RestRequest(resource, method).UseNewtonsoftJson());

            builder.RegisterInstance<Func<Method, IRestRequest>>(
                method => new RestRequest(method).UseNewtonsoftJson());

            builder.RegisterInstance<Func<IRestRequest>>(
                () => new RestRequest(Method.POST).UseNewtonsoftJson());
        }
    }
}