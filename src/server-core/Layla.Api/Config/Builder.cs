using Layla.Infrastructure.Extensions;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Layla.Api.Config;

public static class Builder
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            int.TryParse(builder.Configuration["Ports:HTTPS"] ?? "5288", out int httpsPort);
            int.TryParse(builder.Configuration["Ports:HTTP"] ?? "5287", out int httpPort);

            options.ListenAnyIP(httpsPort, listen =>
            {
                string? certPath = builder.Configuration["ASPNETCORE_Kestrel:Certificates:Default:Path"];
                string certPassword = builder.Configuration["ASPNETCORE_Kestrel:Certificates:Default:Password"] ?? "YourStrongPasswordAF";

                if (string.IsNullOrEmpty(certPath) || !File.Exists(certPath))
                {
                    certPath = "/app/certs_static/aspnetapp.pfx";
                }
                if (!File.Exists(certPath))
                {
                    certPath = "/app/certs/aspnetapp.pfx";
                }
                if (!File.Exists(certPath))
                {
                    certPath = "/certs/aspnetapp.pfx";
                }
                if (!File.Exists(certPath))
                {
                    certPath = Path.Combine(AppContext.BaseDirectory, "Certs", "aspnetapp.pfx");
                }
                if (!File.Exists(certPath))
                {
                    certPath = Path.Combine(Directory.GetCurrentDirectory(), "Certs", "aspnetapp.pfx");
                }

                if (File.Exists(certPath))
                {
                    listen.UseHttps(certPath, certPassword);
                }
                else
                {
                    listen.UseHttps();
                }
            });

            options.ListenAnyIP(httpPort);
        });

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
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
            {{
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                },
                Array.Empty<string>()},
            });

            string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });

        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddSignalR();
        int.TryParse(builder.Configuration["Ports:HTTPS"] ?? "5288", out int httpsPortConf);
        int.TryParse(builder.Configuration["Ports:HTTP"] ?? "5287", out int httpPortConf);
        string bindHost = builder.Environment.IsProduction() ? "+" : "localhost";
        builder.WebHost.UseUrls(
            $"https://{bindHost}:{httpsPortConf};http://{bindHost}:{httpPortConf};");
        builder.Services.AddHealthChecks();
    }
}