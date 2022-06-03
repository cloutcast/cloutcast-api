using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace CloutCast
{
    using Entities;
    using Models;

    public static class ApiHelper
    {
        public static IServiceCollection ConfigureSwagger(this IServiceCollection services, Assembly appAssembly, string description)
        {
            var assemblyName = appAssembly.GetName();
            var title = assemblyName.Name?.Replace(".", " ") ?? "missing assembly name";

            var swaggerSecurityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer", // must be lower case
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            // add Basic Authentication
            var apiKeySecurityScheme = new OpenApiSecurityScheme
            {
                Name = "x-api-key",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme",
                Reference = new OpenApiReference
                {
                    Id = "ApiKey",
                    Type = ReferenceType.SecurityScheme
                }
            };

            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = $"{assemblyName.Version}",
                        Title = title,
                        Description = description,
                        Contact = new OpenApiContact
                        {
                            Name = "dev_awesome",
                            Email = string.Empty
                        }
                    });

//                    c.DescribeAllEnumsAsStrings();

                    c.AddSecurityDefinition(swaggerSecurityScheme.Reference.Id, swaggerSecurityScheme);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {swaggerSecurityScheme, new string[] { }}
                    });
                    
                    c.AddSecurityDefinition(apiKeySecurityScheme.Reference.Id, apiKeySecurityScheme);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {apiKeySecurityScheme, new string[] { }}
                    });
                    
                    // Set the comments path for the Swagger JSON and UI.
                    var xmlFile = $"{appAssembly.GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                })
                .AddSwaggerGenNewtonsoftSupport(); // NewtonSoft!

            return services;
        }

        public static void ConfigureSwaggerUi(SwaggerUIOptions options, Assembly appAssembly)
        {
            var assemblyName = appAssembly.GetName();
            var name = assemblyName.Name ?? "";
            var title = $"{name.Replace(".", " ")} {assemblyName.Version}";

            options.DefaultModelsExpandDepth(-1); // Disable swagger schemas at bottom
            options.SwaggerEndpoint("/swagger/v1/swagger.json", title);
            options.InjectStylesheet($"/swagger-ui/{name}.css");

            /* https://stackoverflow.com/questions/59849045/swagger-ui-for-net-core-3-1-api-is-very-slow */
            options.ConfigObject.AdditionalItems.Add("syntaxHighlight", false); //Turns off syntax highlight which causing performance issues...
        }

        public static AppSource GetApp(this ControllerBase controller) => controller?.HttpContext?.Items["App"] as AppSource;
        
        public static AuthorizedSession GetSession(this ControllerBase controller)
        {
            var source = controller.User;
            return new AuthorizedSession
            {
                App = GetApp(controller),
                User = new BitCloutUser
                {
                    Id = GetValue<long>(source.Claims, "Id"),
                    Handle = GetValue<string>(source.Claims, "name"),
                    PublicKey = GetValue<string>(source.Claims, "PublicKey")
                }
            };
        }

        private static T GetValue<T>(IEnumerable<Claim> claims, string key)
        {
            var match =claims.SingleOrDefault(c => c.Type.Equals(key, StringComparison.CurrentCultureIgnoreCase));
            if (match == null) return default;

            return (T) Convert.ChangeType(match.Value, typeof(T));
        }
    }
}