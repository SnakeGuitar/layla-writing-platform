using Microsoft.Extensions.DependencyInjection;

namespace Layla.Desktop.Services.User;

public static class ServiceLocator
{
    private static IServiceProvider? _provider;

    public static void Initialize(IServiceProvider provider)
    {
        _provider = provider;
    }

    public static T? GetService<T>() where T : class
    {
        return _provider?.GetService<T>();
    }
}
