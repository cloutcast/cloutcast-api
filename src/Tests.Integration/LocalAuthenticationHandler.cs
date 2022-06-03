using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable StringLiteralTypo
namespace CloutCast
{
    using Contracts;
    using Entities;

    public class LocalAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /* https://careers.hcss.com/bypass-authorize-attribute-running-integration-test/ */
        
        /// <summary>
        /// The default user id, injected into the claims for all requests.
        /// </summary>
        public static string UserIdClaimType = "https://cloutcast.io/BitCloutKey";
        public static IBitCloutUser User = new BitCloutUser
        {
            Handle = "awesome_dev",
            Id = 0,
            PublicKey = "BC1YLiRDwmSUdGKgf2Mo7tux31skoi378MckGRGVguzfZWQpcV3eKcp"
        };

        /// <summary>
        /// The name of the authorization scheme that this handler will respond to.
        /// </summary>
        public const string AuthScheme = "LocalAuth";
        private readonly Claim[] _claims;

        public LocalAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) :
            base(options, logger, encoder, clock)
        {
            _claims = new[]
            {
                new Claim("Id", User.Id.ToString()),
                new Claim("PublicKey", User.PublicKey),
                new Claim("name", User.Handle)
            };
        }

        /// <summary>
        /// Marks all authentication requests as successful, and injects the
        /// default company id into the user claims.
        /// </summary>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authenticationTicket = new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(_claims, AuthScheme)),
                new AuthenticationProperties(),
                AuthScheme);
            return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
        }
    }

    public static class LocalAuthHandlerExtensions
    {
        public static AuthenticationBuilder AddLocalAuthentication(this IServiceCollection services) => services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = LocalAuthenticationHandler.AuthScheme;
                options.DefaultChallengeScheme = LocalAuthenticationHandler.AuthScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(
                LocalAuthenticationHandler.AuthScheme, authOptions =>
                {

                });
    }

}