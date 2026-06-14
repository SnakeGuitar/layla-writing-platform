using Layla.Core.Configuration;
using Layla.Core.Interfaces.Services;
using Layla.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Layla.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<PayPalSettings>(configuration.GetSection("PayPal"));

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IDonationService, DonationService>();

        return services;
    }
}
