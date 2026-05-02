using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AccountRequest;

/// <summary>
/// Jellyfin plugin entry point for account request management.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// The stable plugin identifier used by Jellyfin and the plugin repository manifest.
    /// </summary>
    public static readonly Guid PluginGuid = Guid.Parse("a0b260bb-3070-4000-a23d-148f36ab5f08");

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Jellyfin application paths.</param>
    /// <param name="xmlSerializer">Jellyfin XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the active plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Account Request";

    /// <inheritdoc />
    public override Guid Id => PluginGuid;

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = "Account Requests",
                EmbeddedResourcePath = "Jellyfin.Plugin.AccountRequest.Web.adminrequests.html",
                EnableInMainMenu = true,
                MenuSection = "server",
                MenuIcon = "person_add"
            },
            new PluginPageInfo
            {
                Name = "AccountRequestLoginInjector",
                EmbeddedResourcePath = "Jellyfin.Plugin.AccountRequest.Web.logininjector.html",
                EnableInMainMenu = false
            }
        ];
    }
}
