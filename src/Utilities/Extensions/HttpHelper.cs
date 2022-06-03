using System;
using System.Collections.Generic;

namespace CloutCast
{
    public static class HttpHelper
    {
        private static readonly Dictionary<int, Tuple<string, string>> Reasons =
            new Dictionary<int, Tuple<string, string>>
            {
                {100, new Tuple<string, string>("Continue", "indicates that the client can continue with its request")},
                {101, new Tuple<string, string>("Switching Protocols", " indicates that the protocol version or protocol is being changed")},
                {200, new Tuple<string, string>("OK", "indicates that the request succeeded and that the requested information is in the response. This is the most common status code to receive.")},
                {201, new Tuple<string, string>("Created", "indicates that the request resulted in a new resource created before the response was sent.")},
                {202, new Tuple<string, string>("Accepted", "indicates that the request has been accepted for further processing.")},
                {203, new Tuple<string, string>("Non-Authoritative Information", "indicates that the returned meta information is from a cached copy instead of the origin server and therefore may be incorrect.")},
                {204, new Tuple<string, string>("No Content", "indicates that the request has been successfully processed and that the response is intentionally blank.")},
                {205, new Tuple<string, string>("Reset Content", "indicates that the client should reset (not reload) the current resource.")},
                {206, new Tuple<string, string>("Partial Content", "indicates that the response is a partial response as requested by a GET request that includes a byte range.")},
                {300, new Tuple<string, string>("Multiple Choices", "indicates that the requested information has multiple representations. The default action is to treat this status as a redirect and follow the contents of the Location header associated with this response.")},
                {301, new Tuple<string, string>("Moved Permanently", "indicates that the requested information has been moved to the URI specified in the Location header. The default action when this status is received is to follow the Location header associated with the response. When the original request method was POST, the redirected request will use the GET method.")},
                {302, new Tuple<string, string>("Found", "indicates that the requested information is located at the URI specified in the Location header. The default action when this status is received is to follow the Location header associated with the response. When the original request method was POST, the redirected request will use the GET method.")},
                {303, new Tuple<string, string>("See Other", "automatically redirects the client to the URI specified in the Location header as the result of a POST. The request to the resource specified by the Location header will be made with a GET.")},
                {304, new Tuple<string, string>("Not Modified", "indicates that the client's cached copy is up to date. The contents of the resource are not transferred.")},
                {305, new Tuple<string, string>("Use Proxy", "indicates that the request should use the proxy server at the URI specified in the Location header.")},
                {306, new Tuple<string, string>("Unused", "is a proposed extension to the HTTP/1.1 specification that is not fully specified.")},
                {307, new Tuple<string, string>("Temporary Redirect", "indicates that the request information is located at the URI specified in the Location header. The default action when this status is received is to follow the Location header associated with the response. When the original request method was POST, the redirected request will also use the POST method.")},
                {308, new Tuple<string, string>("Permanent Redirect", "indicates that the resource requested has been definitively moved to the URL given by the Location headers.")},
                {400, new Tuple<string, string>("Bad Request", "indicates that the request could not be understood by the server. This is sent when no other error is applicable, or if the exact error is unknown or does not have its own error code.")},
                {401, new Tuple<string, string>("Unauthorized", "indicates that the requested resource requires authentication. The WWW-Authenticate header contains the details of how to perform the authentication.")},
                {402, new Tuple<string, string>("Payment Required","is reserved for future use.")},
                {403, new Tuple<string, string>("Forbidden", "indicates that the server refuses to fulfill the request.")},
                {404, new Tuple<string, string>("Not Found", "indicates that the requested resource does not exist on the server.")},
                {405, new Tuple<string, string>("Method Not Allowed", "indicates that the request method (POST or GET) is not allowed on the requested resource.")},
                {406, new Tuple<string, string>("Not Acceptable", "indicates that the client has indicated with Accept headers that it will not accept any of the available representations of the resource.")},
                {407, new Tuple<string, string>("Proxy Authentication Required", "indicates that the requested proxy requires authentication. The Proxy-authenticate header contains the details of how to perform the authentication.")},
                {408, new Tuple<string, string>("Request Timeout", "indicates that the client did not send a request within the time the server was expecting the request.")},
                {409, new Tuple<string, string>("Conflict", "indicates that the request could not be carried out because of a conflict on the server.")},
                {410, new Tuple<string, string>("Gone", "indicates that the requested resource is no longer available.")},
                {411, new Tuple<string, string>("Length Required", "indicates that the required Content-length header is missing.")},
                {412, new Tuple<string, string>("Precondition Failed", "indicates that a condition set for this request failed, and the request cannot be carried out. Conditions are set with conditional request headers like If-Match, If-None-Match, or If-Unmodified-Since.")},
                {413, new Tuple<string, string>("Payload Too Large", "indicates that the request is too large for the server to process.")},
                {414, new Tuple<string, string>("URI Too Long", "indicates that the URI is too long.")},
                {415, new Tuple<string, string>("Unsupported Media Type", "indicates that the request is an unsupported type.")},
                {416, new Tuple<string, string>("Range Not Satisfiable", "indicates that the range of data requested from the resource cannot be returned, either because the beginning of the range is before the beginning of the resource, or the end of the range is after the end of the resource.")},
                {417, new Tuple<string, string>("Expectation Failed", "indicates that an expectation given in an Expect header could not be met by the server.")},
                {418, new Tuple<string, string>("I'm a teapot", "indicates that the server refuses to brew coffee because it is a teapot")},
                {422, new Tuple<string, string>("Unprocessable Entity", "indicates that the server understands the content type of the request entity, and the syntax of the request entity is correct, but it was unable to process the contained instructions")},
                {425, new Tuple<string, string>("Too Early", "indicates that the server is unwilling to risk processing a request that might be replayed, which creates the potential for a replay attack.")},
                {426, new Tuple<string, string>("Upgrade Required", "indicates that the client should switch to a different protocol such as TLS/1.0.")},
                {428, new Tuple<string, string>("Precondition Required", "indicates that the server requires the request to be conditional. Typically, this means that a required precondition header, such as If-Match, is missing")},
                {429, new Tuple<string, string>("Too Many Requests", "indicates the user has sent too many requests in a given amount of time ('rate limiting')")},
                {431, new Tuple<string, string>("Request Header Fields Too Large", "indicates that the server is unwilling to process the request because its header fields are too large. The request may be resubmitted after reducing the size of the request header fields.")},
                {451, new Tuple<string, string>("Unavailable For Legal Reasons", "indicates that the user requested a resource that is not available due to legal reasons.")},
                {500, new Tuple<string, string>("Internal Server Error", "indicates that a generic error has occurred on the server.")},
                {501, new Tuple<string, string>("Not Implemented", "indicates that the server does not support the requested function.")},
                {502, new Tuple<string, string>("Bad Gateway", "indicates that an intermediate proxy server received a bad response from another proxy or the origin server.")},
                {503, new Tuple<string, string>("Service Unavailable", "indicates that the server is temporarily unavailable, usually due to high load or maintenance.")},
                {504, new Tuple<string, string>("Gateway Timeout", "indicates that an intermediate proxy server timed out while waiting for a response from another proxy or the origin server.")},
                {505, new Tuple<string, string>("HTTP Version Not Supported", "indicates that the requested HTTP version is not supported by the server.")},
                {511, new Tuple<string, string>("Network Authentication Required", "indicates that the client needs to authenticate to gain network access. This status is not generated by origin servers, but by intercepting proxies that control access to the network.")}
            };

        public static string GetReasonPhrase(int status)
        {
            if (!Reasons.ContainsKey(status))
                return $"unknown status {status}";

            var tpl = Reasons[status];
            return tpl.Item1;
        }

        public static string GetStatusDescription(int status)
        {
            if (!Reasons.ContainsKey(status))
                return $"unknown status {status}";

            var tpl = Reasons[status];
            return tpl.Item2;
        }
    }
}