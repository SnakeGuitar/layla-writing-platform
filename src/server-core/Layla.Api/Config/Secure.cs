using Layla.Api.Filters;
using Layla.Api.Middleware;
using Layla.Core.Constants;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Text.Unicode;
using MongoDB.Bson.IO;
using System.Buffers.Text;

namespace Layla.Api.Config;

public static class Secure
{
    public static void Configure(WebApplicationBuilder builder)
    {
        string jwtSecret = RequireConfig(builder, "JwtSettings:Secret");

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

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        builder.Services.AddScoped<TokenVersionValidator>();
        builder.Services.AddScoped<RequireUserIdFilter>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Bearer";
            options.DefaultChallengeScheme = "Bearer";
        })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    NameClaimType = ClaimNames.Name,
                    RoleClaimType = ClaimNames.Role,
                    IssuerSigningKey = new SymmetricSecurityKey(new UTF8Encoding().GetBytes(jwtSecret))
                };
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hubs/voice") || path.StartsWithSegments("/hubs/presence")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var validator = context.HttpContext.RequestServices.GetRequiredService<TokenVersionValidator>();
                        await validator.ValidateAsync(context);
                    }
                };
            });

        builder.Services.AddAuthorization();

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

    public static string RequireConfig(WebApplicationBuilder builder, string key)
    {
        var value = builder.Configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Missing required configuration '{key}'. Set it via environment variable or user-secrets.");
        return value;
    }
}