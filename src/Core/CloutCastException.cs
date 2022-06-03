using System;
using System.Net;

namespace CloutCast
{
    public class CloutCastException : Exception
    {
        public ErrorModel Error { get; } = new ErrorModel();

        public CloutCastException(string message) : base(message) => Error.Message = message;
        public CloutCastException(string message, int status): this(message) => Error.StatusCode = status;
        public CloutCastException(string message, HttpStatusCode status) : this(message, (int)status) {}

        public CloutCastException(HttpStatusCode status, params string[] reasons)
        {
            Error.StatusCode = (int) status;
            Error.Message = HttpHelper.GetReasonPhrase(Error.StatusCode);
            Error.Reasons.AddRange(reasons);
        }

        public CloutCastException(ErrorModel error) => Error = error;

    }
}