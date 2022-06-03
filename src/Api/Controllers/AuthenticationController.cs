using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CloutCast.Controllers
{
    using Entities;
    using Options;
    using Requests;

    [ApiController, Route("authenticate")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private readonly ILog _logger;

        public AuthenticationController(IMediator mediator, IConfiguration configuration, ILog logger)
        {
            _mediator = mediator;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Returns Valid Auth Token
        /// </summary>
        /// <param name="publicKey">BitClout PublicKey</param>
        /// <param name="handle">Current BitClout Handle</param>
        [HttpPost, Route("")]
        public async Task<string> Authenticate(string publicKey, string handle)
        {
            _logger.Info($"Authenticate; Handle={handle}; PublicKey={publicKey}");
            var user = await _mediator.Send<SaveUserRequest, BitCloutUser>(r =>
            {
                r.App = this.GetApp();
                r.Handle = handle;
                r.PublicKey = publicKey;
            });

            return ToJavaWebToken(GetOptions(), user);
        }

        protected internal AuthenticationOption GetOptions() => _configuration
            .GetSection("Authentication")
            .Get<AuthenticationOption>();

        protected internal string ToJavaWebToken(AuthenticationOption option, BitCloutUser user)
        {
            //https://www.yogihosting.com/jwt-api-aspnet-core/
            //https://www.yogihosting.com/jwt-api-aspnet-core/#secure-api
            var securityKey = option.ToSecurityKey();
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                audience: option.Audience,
                issuer: option.Issuer,
                expires: DateTime.Now.AddHours(12),
                signingCredentials: credentials,
                claims: new[]
                {
                    new Claim("Id", user.Id.ToString()),
                    new Claim("PublicKey", user.PublicKey),
                    new Claim(JwtRegisteredClaimNames.Name, user.Handle),
                }
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}