using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class AuthExtention
    {
        public static IServiceCollection AddKeyCloakAuthentication(this IServiceCollection services) {

            services.AddAuthentication()
                .AddKeycloakJwtBearer("keycloak", realm: "overflow", options =>
                {
                    options.Audience = "overflow";
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuers = new List<string> { "http://localhost:6001/realms/overflow", "http://keycloak/realms/overflow", "http://id.local.keycloak/realms/overflow" }
                    };
                });

            return services;

        }
    }
}
