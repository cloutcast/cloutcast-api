using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloutCast
{
    public class ResponseEnvelopeResultExecutor : ObjectResultExecutor
    {
        private static readonly AssemblyName ResultAssembly = typeof(ResponseEnvelopeResultExecutor).Assembly.GetName();

        /*
         * https://stackoverflow.com/questions/47181356/c-sharp-dotnet-core-middleware-wrap-response
         */
        
        private readonly SessionKeyModel _sessionKey;

        public ResponseEnvelopeResultExecutor(
            SessionKeyModel sessionKey,
            OutputFormatterSelector formatterSelector, 
            IHttpResponseStreamWriterFactory writerFactory, 
            ILoggerFactory loggerFactory, 
            IOptions<MvcOptions> mvcOptions) : base(formatterSelector, writerFactory, loggerFactory, mvcOptions)
        {
            _sessionKey = sessionKey;
        }

        public override Task ExecuteAsync(ActionContext context, ObjectResult result)
        {
            var val = result.Value;
            var response = ResponseWrapper.SealEnvelope(context.HttpContext, _sessionKey, val);
            if (val == null)
                result.Value = response;
            else
            {
                var typeCode = Type.GetTypeCode(result.Value.GetType());
                if (typeCode == TypeCode.Object) result.Value = response;
            }

            return base.ExecuteAsync(context, result);
        }
    }

    public static class ResponseWrapper
    {
        private static readonly AssemblyName ResultAssembly = typeof(ResponseWrapper).Assembly.GetName();

        public static ResponseEnvelope<ErrorModel> SealEnvelope(HttpContext context, SessionKeyModel sessionKey, ErrorModel error)
        {
            if (error.Data?.None() ?? false) error.Data = null;
            if (error.Reasons?.None() ?? false) error.Reasons = null;

            return new ResponseEnvelope<ErrorModel>
            {
                ApiVersion = ResultAssembly.Version?.ToString() ?? "missing version",
                Error = error,
                RequestUrl = context.Request.Path,
                SessionKey = sessionKey.ToString()
            };
        }

        public static ResponseEnvelope<T> SealEnvelope<T>(HttpContext context, SessionKeyModel sessionKey, T data) where T : class =>
            new ResponseEnvelope<T>
            {
                ApiVersion = ResultAssembly.Version?.ToString() ?? "missing version",
                Count = data is ICollection list ? list.Count : (long?) null,
                Data = data,
                RequestUrl = context.Request.Path,
                SessionKey = sessionKey.ToString()
            };
    }

    public class ResponseEnvelope<T>
    {
        public string ApiVersion { get; set; }
        public string RequestUrl { get; set; }
        public string SessionKey { get; set; }
        public long? Count { get; set; }
        public T Data { get; set; }
        public ErrorModel Error { get; set; }
    }
}