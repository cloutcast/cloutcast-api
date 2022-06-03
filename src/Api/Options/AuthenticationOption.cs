using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CloutCast.Options
{
    public class AuthenticationOption
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public string SigningKey { get; set; }

        public SymmetricSecurityKey ToSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
    }
}