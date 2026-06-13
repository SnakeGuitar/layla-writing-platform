using Layla.Api.Filters;
using Layla.Api.Middleware;
using Layla.Core.Constants;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Layla.Api.Config;

public static class Secure
{
    const int MIN_JWT_SECRET_LENGTH = 32;

    public static void Configure(WebApplicationBuilder builder)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        builder.Services.AddScoped<TokenVersionValidator>();
        builder.Services.AddScoped<RequireUserIdFilter>();
        string jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
        if (String.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < MIN_JWT_SECRET_LENGTH)
            throw new InvalidOperationException($"'JwtSettings:Secret' must be at least {MIN_JWT_SECRET_LENGTH} characters for HS256 security.");

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Bearer";
            options.DefaultChallengeScheme = "Bearer";
        })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    NameClaimType = ClaimNames.Name,
                    RoleClaimType = ClaimTypes.Role,
                    IssuerSigningKey = new SymmetricSecurityKey(new UTF8Encoding().GetBytes(jwtSecret))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // TODO-Desarrollo: Verificar acceso a SignalR services
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hubs/voice") ||
                            path.StartsWithSegments("/hubs/presence") ||
                            path.StartsWithSegments("/hubs/manuscript")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var validator = context.HttpContext.RequestServices.GetRequiredService<TokenVersionValidator>();
                        await validator.ValidateAsync(context);
                        ExpandRoleArrayClaims(context.Principal);
                    }
                };
            });

        builder.Services.AddAuthorization();


        builder.Services.AddCors(options =>
        {
            options.AddPolicy("LaylaCors", policy =>
            {
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.AddSlidingWindowLimiter("login", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 3;
                opt.PermitLimit = 10;
                opt.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }

    private static void ExpandRoleArrayClaims(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
            return;

        foreach (var roleClaim in principal.FindAll(ClaimNames.Role).ToList())
        {
            var value = roleClaim.Value.Trim();
            if (!value.StartsWith("[", StringComparison.Ordinal))
                continue;

            try
            {
                var roles = JsonSerializer.Deserialize<string[]>(value) ?? [];
                foreach (var role in roles)
                {
                    if (!string.IsNullOrWhiteSpace(role) &&
                        !principal.HasClaim(ClaimTypes.Role, role))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }
            catch (JsonException)
            {
                // Leave malformed role claims untouched; normal authorization will reject them.
            }
        }
    }
}
