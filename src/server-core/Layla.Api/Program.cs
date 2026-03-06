using Layla.Api.Extensions;
using Layla.Api.Middleware;
using Layla.Core.Entities;
using Layla.Core.Extensions;
using Layla.Infrastructure.Data;
using Layla.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Layla API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

builder.Services.AddInfrastructureServices(builder.Configuration);

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
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
            NameClaimType = "name",
            RoleClaimType = "role",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
        };
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var principal = context.Principal;
                if (principal == null) 
                {
                    context.Fail("No principal.");
                    return;
                }

                var userId = principal.FindFirst("sub")?.Value 
                             ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var tokenVersionClaim = principal.FindFirst("token_version")?.Value;

                if (!string.IsNullOrEmpty(userId) && int.TryParse(tokenVersionClaim, out int tokenVersion))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null || user.TokenVersion != tokenVersion)
                    {
                        context.Fail("Session expired. User logged in from another device.");
                    }
                    else
                    {
                        var identity = principal.Identity as ClaimsIdentity;
                        if (identity != null && !principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
                        }
                    }
                }
                else
                {
                    context.Fail("Invalid token structure (missing user identity or TokenVersion).");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCoreServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { "Writer", "Editor", "Reader", "Admin" };

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
        logger.LogError(ex, "Error at creating database or roles");
    }
}

app.Run();