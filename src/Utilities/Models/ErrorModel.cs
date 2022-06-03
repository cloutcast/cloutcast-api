using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;

namespace CloutCast
{
    public class ErrorModel
    {
        public ErrorModel() { }

        public ErrorModel(IEnumerable<ValidationFailure> failures)
        {
            Message = "Validation failure";
            foreach (var failure in failures)
            {
                Reasons.Add(failure.ErrorMessage);
                if (failure.CustomState is int statusCode && statusCode > StatusCode)
                    StatusCode = statusCode;
            }

            if (StatusCode == 0) StatusCode = (int) HttpStatusCode.Forbidden;
        }

        public string Message { get; set; }
        public int StatusCode { get; set; }
        public List<string> Reasons { get; set; } = new List<string>();
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}