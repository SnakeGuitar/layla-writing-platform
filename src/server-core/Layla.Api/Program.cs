using Layla.Api.Config;
using Layla.Api.Hubs;
using Layla.Api.Middleware;
using Layla.Core.Constants;
using Layla.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    Secrets.Configure(builder);
}

Builder.Configure(builder);
Services.Configure(builder);
Secure.Configure(builder);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("LaylaCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// HTTPS redirection is intentionally NOT enabled in production:
// the API Gateway (YARP) acts as the TLS terminator. Internal traffic
// between gateway and service is HTTP within the private network.

app.MapHealthChecks("/health");
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
        ApplicationDbContext? context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = AppRoles.All;

        foreach (String role in roles)
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