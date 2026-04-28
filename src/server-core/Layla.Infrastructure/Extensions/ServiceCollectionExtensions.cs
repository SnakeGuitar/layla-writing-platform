using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Queue;
using Layla.Core.Interfaces.Services;
using Layla.Infrastructure.Data;
using Layla.Infrastructure.Data.Repositories;
using Layla.Infrastructure.Queue;
using Layla.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Layla.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        Int32 commandTimeout = configuration.GetValue<int>(
            "DatabaseConfigs:SQL:CommandTimeoutSeconds"
        );
        Int32 maxRetryCount = configuration.GetValue<int>(
            "DatabaseConfigs:SQL:MaxRetryCount"
        );
        TimeSpan maxRetryDelay = configuration.GetValue<TimeSpan>(
            "DatabaseConfigs:SQL:MaxRetryDelay"
        );
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetValue<string>("DatabaseConfigs:SQL:ConnectionString"),
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(commandTimeout);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: maxRetryCount,
                        maxRetryDelay: maxRetryDelay,
                        errorNumbersToAdd: null
                    );
                }
            );

            options.ConfigureWarnings(w =>
                w.Log(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 12;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAppUserRepository, AppUserRepository>();

        // Register EventBus as a single instance shared by both interfaces
        services.AddSingleton<Connection>();
        services.AddSingleton<IPublisher>(sp => sp.GetRequiredService<Publisher>());


        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
