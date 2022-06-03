using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CloutCast
{
    using Entities;
    using Requests;
    
    public class ApiKeyMiddleware : IMiddleware
    {
        public static string ApiKeyHeaderName = "x-api-key";
        public static string ClientIdHeaderName = "ClientId";

        private readonly IMediator _mediator;
        private readonly ILog _logger;

        public ApiKeyMiddleware(IMediator mediator, ILog log)
        {
            _mediator = mediator;
            _logger = log;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var apiKey = GetKeyFromHeader(context);

            var app = await FindAuthorizingApp(apiKey);

            context.Items.Add("App", app);
            await next(context);
        }

        protected async Task<AppSource> FindAuthorizingApp(string apiKey)
        {
            var allApps = await _mediator.Send(new GetAllAppSourcesRequest());
            var match = allApps.SingleOrDefault(a => a.ApiKey.Equals(apiKey, StringComparison.CurrentCultureIgnoreCase));
            if (match == null)
                throw new CloutCastException(HttpStatusCode.Unauthorized, "ApiKey is not among the registered keys");

            return match;
        }

        protected ClaimsIdentity GenerateIdentities(AppSource app) => new ClaimsIdentity(new List<Claim>
        {
            new Claim("App_Id", app.Id.ToString()),
            new Claim("App_ApiKey", app.ApiKey),
            new Claim("App_Name", app.Name),
            new Claim("App_Company", app.Company)
        });
        protected ClaimsPrincipal GenerateClaim(AppSource app)
        {
            return new ClaimsPrincipal(GenerateIdentities(app));
        }

        protected string GetKeyFromHeader(HttpContext context)
        {
            var request = context.Request;
            var hasApiKeyHeader = request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValue);
            
            if (!hasApiKeyHeader)
                throw new CloutCastException(HttpStatusCode.Unauthorized, "ApiKey was not found");

            _logger.Debug($"Found the header {ApiKeyHeaderName}. Starting API Key validation");

            if (apiKeyValue.None() || string.IsNullOrWhiteSpace(apiKeyValue))
                throw new CloutCastException(HttpStatusCode.Unauthorized, "ApiKey is null or empty");
            return apiKeyValue;
        }
    }
}