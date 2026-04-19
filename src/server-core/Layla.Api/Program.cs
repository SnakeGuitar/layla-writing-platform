using Layla.Api.Config;
using Layla.Api.Hubs;
using Layla.Api.Middleware;
using Layla.Core.Constants;
using Layla.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -- Fail-fast on missing critical secrets -------------------------------------
// Secrets MUST be supplied via environment variables, dotnet user-secrets, or a
// secret store — NEVER committed to appsettings.json. Without this guard the
// app would boot with null!/empty strings and crash deep in JWT validation
// or DB connection paths.

Secrets.Configure(builder);
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
else
{
    app.UseHttpsRedirection();
}

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