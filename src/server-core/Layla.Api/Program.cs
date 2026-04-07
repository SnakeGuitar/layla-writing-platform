using Layla.Api.Extensions;
using Layla.Api.Filters;
using Layla.Api.Hubs;
using Layla.Api.Middleware;
using Layla.Core.Constants;
using Layla.Core.Entities;
using Layla.Core.Extensions;
using Layla.Core.Interfaces;
using Layla.Infrastructure.Data;
using Layla.Infrastructure.Extensions;
using Layla.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Fail-fast on missing critical secrets ─────────────────────────────────────
// Secrets MUST be supplied via environment variables, dotnet user-secrets, or a
// secret store — NEVER committed to appsettings.json. Without this guard the
// app would boot with null!/empty strings and crash deep in JWT validation
// or DB connection paths.
const int MinJwtSecretLength = 32;

string RequireConfig(string key)
{
    var value = builder.Configuration[key];
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException(
            $"Missing required configuration '{key}'. Set it via environment variable or user-secrets.");
    return value;
}

var jwtSecret = RequireConfig("JwtSettings:Secret");
if (jwtSecret.Length < MinJwtSecretLength)
    throw new InvalidOperationException(
        $"'JwtSettings:Secret' must be at least {MinJwtSecretLength} characters for HS256 security.");

_ = RequireConfig("ConnectionStrings:DefaultConnection");
_ = RequireConfig("RabbitMQ:UserName");
_ = RequireConfig("RabbitMQ:Password");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Layla API",
        Version = "v1",
        Description =
            "Core API for the Layla collaborative writing platform. " +
            "Handles authentication, user management, and project management. " +
            "Manuscript and worldbuilding operations are served by the separate Worldbuilding Service.",
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token. Obtain one via POST /api/tokens. Enter: Bearer <token>",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IVoiceRoomManager, VoiceRoomManager>();
builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();

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

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
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
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSecret))
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
builder.Services.AddCoreServices(builder.Configuration);

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

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("LaylaCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<VoiceHub>("/hubs/voice");
app.MapHub<PresenceHub>("/hubs/presence");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = AppRoles.All;

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Fatal error during database migration or role seeding. Aborting startup.");
        throw;
    }
}

app.Run();