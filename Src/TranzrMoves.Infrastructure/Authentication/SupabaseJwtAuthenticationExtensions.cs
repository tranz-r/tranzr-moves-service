using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Authentication;

public static class SupabaseJwtAuthenticationExtensions
{
    public static IServiceCollection AddSupabaseJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentBusinessUserContext, CurrentBusinessUserContext>();
        services.AddScoped<IAuthorizationHandler, BusinessUserAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, BusinessOwnerAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.BusinessUser, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(new BusinessUserRequirement()));

            options.AddPolicy(AuthorizationPolicies.BusinessOwner, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(new BusinessOwnerRequirement()));
        });

        if (environment.IsEnvironment("Testing"))
        {
            return services;
        }

        var issuer = configuration["SUPABASE_JWT_ISSUER"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException(
                "Missing JWT issuer configuration. Set SUPABASE_JWT_ISSUER.");
        }

        const string audience = "authenticated";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.UseSecurityTokenValidators = true;
                options.RefreshOnIssuerKeyNotFound = true;
                options.Authority = issuer;
                options.MetadataAddress = $"{issuer}/.well-known/openid-configuration";
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.Audience = audience;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };
            });

        return services;
    }
}
