using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using log4net;
using Newtonsoft.Json;
using Polly;

using RestSharp;

namespace CloutCast
{
    using Options;

    public enum BitCloutEndPoints
    {
        MoneyScan,
        SinglePost,
        UserCoinPrice,
        UserFollowerCount,
        UserPosts
    }

    public interface IBitCloutRestFactory
    {
        IRestClient CreateClient(BitCloutEndPoints endPoint, Action<IRestClient> setup = null);
        Policy<IRestResponse<T>> CreatePolicy<T>(int retryAttempt);
        T Execute<T>(IRestClient client, Policy<IRestResponse<T>> policy, object body);
    }

    public class BitCloutRestFactory : IBitCloutRestFactory
    {
        private readonly Func<IRestClient> _clientFactory;
        private readonly Func<IRestRequest> _getRequest;
        private readonly BitCloutOption _options;
        private readonly ILog _logger;

        public BitCloutRestFactory(Func<IRestClient> clientFactory, Func<IRestRequest> getRequest, BitCloutOption options, ILog logger)
        {
            _clientFactory = clientFactory;
            _getRequest = getRequest;
            _options = options;
            _logger = logger;
        }

        public IRestClient CreateClient(BitCloutEndPoints endPoint, Action<IRestClient> setup =null)
        {
            var key = $"{endPoint}";
            if (!_options.Uris.ContainsKey(key))
                throw new CloutCastException(new ErrorModel
                {
                    Data = new Dictionary<string, object> {{"endPoint", key}},
                    Message = "Missing BitClout URI for endPoint",
                    StatusCode = (int) HttpStatusCode.NotFound
                });
            
            var url = _options.Uris[key];
            
            var client = _clientFactory.Invoke();
            client.BaseUrl = new Uri(url);
            client.Timeout = 900000;
            client.AddDefaultHeaders(new Dictionary<string, string>
            {
                {"Referer", "https://bitclout.com/"},
                {"access-control-allow-headers", "Origin, X-Requested-With, Content-Type, Accept"},
                {"Accept", "application/json, text/plain"},
                {"accept-language", "en-US,en;q=0.9"},
                {"origin", "https://bitclout.com/"},
                {"sec-fetch-dest", "empty"},
                {"sec-fetch-mode", "cors"},
                {"sec-fetch-site", "same-site"},
                {"Content-Type", "application/json"}
            });
            setup?.Invoke(client);
            return client;
        }

        public Policy<IRestResponse<T>> CreatePolicy<T>(int retryAttempt)
        {
            var wait = new Func<int, TimeSpan>(attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            return Policy
                .HandleResult<IRestResponse<T>>(response => response == null || response.StatusCode != HttpStatusCode.OK)
                .WaitAndRetry(retryAttempt, wait);
        }

        public T Execute<T>(IRestClient client, Policy<IRestResponse<T>> policy, object body)
        {
            var requestBody = JsonConvert.SerializeObject(body);

            var parsed = policy
                .Execute(() =>
                {
                    _logger.Info("Getting transactions from BitClout");

                    var req = _getRequest.Invoke();
                    req.AddParameter("application/json", requestBody, ParameterType.RequestBody);

                    var stopwatch = Stopwatch.StartNew();
                    var resp = client.Execute(req);
                    stopwatch.Stop();

                    LogRequest(client, req, resp, stopwatch.Elapsed);

                    if (resp.ErrorMessage.IsNotEmpty())
                        _logger.Error(resp.ErrorMessage);

                    return client.Deserialize<T>(resp);
                });

            return parsed == null ? default : parsed.Data;
        }

        private void LogRequest(IRestClient client, IRestRequest request, IRestResponse response, TimeSpan elapsed)
        {
            _logger.Info($"Completed request. Elapsed time: {elapsed}");

            var reqResp = new
            {
                Request = new
                {
                    resource = request.Resource,
                    parameters = request.Parameters.Select(parameter => new
                    {
                        name = parameter.Name,
                        value = parameter.Value,
                        type = parameter.Type.ToString()
                    }),
                    method = request.Method.ToString(),
                    uri = client.BuildUri(request),
                },
                Response = new
                {
                    statusCode = response.StatusCode,
                    headers = response.Headers,
                    responseUri = response.ResponseUri,
                    errorMessage = response.ErrorMessage,
                }
            };
            
            _logger.Debug(JsonConvert.SerializeObject(reqResp));
        }
    }
}