using Layla.Infrastructure.Extensions;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

namespace Layla.Api.Config;

public static class Builder
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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
    }
}