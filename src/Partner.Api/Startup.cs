using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CloutCast
{
    using Options;

    public class PartnerStartup
    {
        private ILifetimeScope _autofacContainer;

        public PartnerStartup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var authOptions = Configuration.GetSection("Authentication").Get<AuthenticationOption>();
            ConfigureAuthorization(services, authOptions);

            IdentityModelEventSource.ShowPII = true; //Add this line

            services
                .AddOptions()
                .Configure<RouteOptions>(options => options.ConstraintMap.Add("activeFlag", typeof(ActiveFlag)));

            services
                .AddSingleton<IActionResultExecutor<ObjectResult>, ResponseEnvelopeResultExecutor>()
                .AddMemoryCache()
                .AddHttpClient() // register the .net core IHttpClientFactory 
                .AddControllers().AddControllersAsServices().AddNewtonsoftJson(options =>
                {
                    var configure = _autofacContainer.Resolve<Action<JsonSerializerSettings>>();
                    configure(options.SerializerSettings);
                })
                .AddFluentValidation(fv =>
                {
                    fv.RegisterValidatorsFromAssemblies(new[] {Program.LogicAssembly, Program.CoreAssembly});
                    fv.RunDefaultMvcValidationAfterFluentValidationExecutes = false;
                });

            services.ConfigureSwagger(Program.ThisAssembly, "Clout.Cast Partner Api");
        }

        protected void ConfigureAuthorization(IServiceCollection services, AuthenticationOption settings) =>
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        //https://stackoverflow.com/questions/45686477/jwt-on-net-core-2-0/45723392#45723392
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = settings.ToSecurityKey(),
                        ValidAudience = settings.Audience,
                        ValidIssuer = settings.Issuer,

                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true
                    };
                });

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            _autofacContainer = app.ApplicationServices.GetAutofacRoot();

            loggerFactory.AddLog4Net("log4net.config");
            app
                .UseStaticFiles()
                .UseSwagger()
                .UseSwaggerUI(c => ApiHelper.ConfigureSwaggerUi(c, Program.ThisAssembly))
                .UseHttpsRedirection()
                .UseMiddleware<ErrorHandlerMiddleware>()
                .UseMiddleware<ApiKeyMiddleware>()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
