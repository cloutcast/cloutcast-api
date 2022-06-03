using System;
using FluentValidation;

namespace CloutCast
{
    public static class FluentValidatorExtensions
    {
        /*
         * https://stackoverflow.com/questions/36562243/not-sure-how-to-test-this-net-string-with-fluentvalidation
         */
        public static IRuleBuilderOptions<T, string> Url<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            bool UrlIsValidUri(string url) => Uri.TryCreate(url, UriKind.Absolute, out var outUri)
                                              && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
            return ruleBuilder.Must(UrlIsValidUri);
        }

    }
}