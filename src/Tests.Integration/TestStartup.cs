using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CloutCast.Options;

namespace CloutCast
{
    public class TestStartup : Startup
    {
        public TestStartup(IConfiguration configuration) : base(configuration)
        {
        }

        protected override void ConfigureAuthorization(IServiceCollection services, AuthenticationOption authentication)
        {
            services.AddLocalAuthentication();
        }
    }
}