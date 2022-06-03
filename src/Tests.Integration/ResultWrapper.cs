using System;

namespace CloutCast
{
    public class ResultWrapper<T>
    {
        public Version ApiVersion { get; set; }
        public string RequestUrl { get; set; }
        public string SessionKey { get; set; }
        public T Data { get; set; }
        public ErrorModel Error { get; set; }
    }
}