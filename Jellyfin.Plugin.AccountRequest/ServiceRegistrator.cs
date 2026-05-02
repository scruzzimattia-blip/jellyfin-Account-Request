using Jellyfin.Plugin.AccountRequest.Api;
using Jellyfin.Plugin.AccountRequest.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AccountRequest;

/// <summary>
/// Registers Account Request plugin services with Jellyfin's dependency injection container.
/// </summary>
public class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<RequestStore>();
        serviceCollection.AddTransient<AccountRequestController>();
    }
}
