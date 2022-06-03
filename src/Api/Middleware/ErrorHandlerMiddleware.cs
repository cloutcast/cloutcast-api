using System;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;

namespace CloutCast
{
    public class ErrorHandlerMiddleware : IMiddleware
    {
        private readonly ILog _log;
        private readonly SessionKeyModel _sessionKey;
        private readonly JsonSerializerSettings _serializerSettings;

        public ErrorHandlerMiddleware(SessionKeyModel sessionKey, JsonSerializerSettings serializerSettings, ILog log)
        {
            _sessionKey = sessionKey;
            _serializerSettings = serializerSettings;
            _log = log;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            Start(context);

            try
            {
                await next(context);

                var model = ToError(context);
                if (model != null) await ProcessError(context, model);
            }
            catch (ValidationException ve)
            {
                await ProcessError(context, new ErrorModel(ve.Errors));
            }
            catch (SqlException sqlEx)
            {
                await ProcessError(context,
                    new ErrorModel {StatusCode = sqlEx.Number - 50000, Message = sqlEx.Message});
            }
            catch (CloutCastException cce)
            {
                await ProcessError(context, cce.Error);
            }
            catch (RestSharp.DeserializationException de)
            {
                await ProcessError(context, new ErrorModel
                {
                    StatusCode = (int) HttpStatusCode.NotAcceptable,
                    Message = (de.InnerException ?? de).Message
                });
            }
            catch (Exception ex)
            {
                var errorModel = ex.Data.Contains("ErrorModel")
                    ? (ErrorModel) ex.Data["ErrorModel"]
                    : new ErrorModel {StatusCode = 500, Message = ex.Message};

                await ProcessError(context, errorModel);
            }
        }

        protected internal ErrorModel ToError(HttpContext context)
        {
            var errorModel = new ErrorModel
            {
                StatusCode = context.Response.StatusCode, 
            };
            switch (errorModel.StatusCode)
            {
                case var n when (n < 400): return null;
                case 401: 
                    errorModel.Message = "Unauthorized attempt";
                    break;
                default:
                    errorModel.Message = HttpHelper.GetReasonPhrase(errorModel.StatusCode);
                    break;
            }
            return errorModel;
        }

        protected internal Task ProcessError(HttpContext context, ErrorModel error)
        {
            _log.Error(error.Reasons.Aggregate($"Finished; [{error.StatusCode}]; Message={error.Message}",
                (c, r) => c + $"; Reason={r}"));

            context.Response.StatusCode = error.StatusCode;
            context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = HttpHelper.GetReasonPhrase(error.StatusCode);
            context.Response.ContentType = "application/json";

            var sealedError = ResponseWrapper.SealEnvelope(context, _sessionKey, error);
            var json = JsonConvert.SerializeObject(sealedError, _serializerSettings);
            return context.Response.WriteAsync(json);
        }
        
        protected internal void Start(HttpContext context)
        {
            var key = LogicalThreadContext.Properties[SessionKeyModel.Name];
            if (key != null)
                _log.Error($"SessionKey already generated; SessionKey={key}; OwinContext={context};");
            else
            {
                context.Items[SessionKeyModel.Name] = _sessionKey;
                context.Response.Headers[SessionKeyModel.HeaderName] = _sessionKey.ToString();
                LogicalThreadContext.Properties[SessionKeyModel.Name] = _sessionKey;
            }
        }
    }
}