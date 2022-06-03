using System;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using log4net;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CloutCast.Handlers
{
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetJsonWebKeySetHandler : IRequestHandler<GetJsonWebKeySetRequest, string>
    {
        private readonly IMemoryCache _cache;
        private readonly IFileSystem _fileSystem;
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;
        private readonly GetJsonWebKeySetRequestValidator _validator;

        public GetJsonWebKeySetHandler(IFileSystem fileSystem, IMemoryCache cache, HttpClient httpClient, GetJsonWebKeySetRequestValidator validator, ILog logger)
        {
            _fileSystem = fileSystem;
            _cache = cache;
            _httpClient = httpClient;
            _validator = validator;
            _logger = logger;
        }

        private static string _fileName = "jwks2.json";
        public async Task<string> Handle(GetJsonWebKeySetRequest request, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(request, cancellationToken);
            
            //Get from cache
            if (_cache.TryGetValue(_fileName, out string json))
                return json;

            var fullFileName = GetFileName(request.CachePath);

            if (!request.ForceFetch && DoesFileExist(fullFileName))
                json = ReadFile(fullFileName);

            else
            {
                json = await GetFromWeb(request.KeySetUrl);
                WriteFile(fullFileName, json);
            }

            _cache.Set(_fileName, json);

            return json;
        }

        protected internal string GetFileName(string localPath) => Path.Combine(localPath, _fileName);
        protected internal bool DoesFileExist(string fullFileName) => _fileSystem.File.Exists(fullFileName);

        protected internal async Task<string> GetFromWeb(string url)
        {
            _logger.Info($"Get webKeySet from url; {url}");
            _httpClient.BaseAddress = new Uri(url);
            var result = await _httpClient.GetAsync("");
            var body = await result.Content.ReadAsStringAsync();

            return body;
        }

        protected internal string ReadFile(string fileName) => _fileSystem.File.ReadAllText(fileName);
        protected internal void WriteFile(string fullFile, string json) => _fileSystem.File.WriteAllText(fullFile, json);
    }
}