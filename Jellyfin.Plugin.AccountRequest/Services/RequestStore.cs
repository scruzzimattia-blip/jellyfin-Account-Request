using System.Text.Json;
using AccountRequestModel = Jellyfin.Plugin.AccountRequest.Models.AccountRequest;

namespace Jellyfin.Plugin.AccountRequest.Services;

/// <summary>
/// Persists account requests to a JSON file in the plugin data directory.
/// </summary>
public class RequestStore
{
    private const string RequestsFileName = "requests.json";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _syncRoot = new();
    private readonly string _requestsFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestStore"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the plugin instance is unavailable.</exception>
    public RequestStore()
        : this(GetPluginDataPath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestStore"/> class with a specific data path.
    /// </summary>
    /// <param name="pluginDataPath">The plugin data directory path.</param>
    public RequestStore(string pluginDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDataPath);

        Directory.CreateDirectory(pluginDataPath);
        _requestsFilePath = Path.Combine(pluginDataPath, RequestsFileName);
    }

    /// <summary>
    /// Gets all stored account requests.
    /// </summary>
    /// <returns>All persisted account requests.</returns>
    public IReadOnlyList<AccountRequestModel> GetAll()
    {
        lock (_syncRoot)
        {
            return ReadRequests();
        }
    }

    /// <summary>
    /// Adds a new account request to the store.
    /// </summary>
    /// <param name="request">The request to add.</param>
    /// <returns>The stored request.</returns>
    public AccountRequestModel Add(AccountRequestModel request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_syncRoot)
        {
            var requests = ReadRequests();
            requests.Add(request);
            WriteRequests(requests);
            return request;
        }
    }

    /// <summary>
    /// Updates the status of an existing account request.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="status">The new request status.</param>
    /// <returns><c>true</c> when the request was found and updated; otherwise, <c>false</c>.</returns>
    public bool UpdateStatus(Guid id, string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        lock (_syncRoot)
        {
            var requests = ReadRequests();
            var request = requests.FirstOrDefault(item => item.Id == id);
            if (request is null)
            {
                return false;
            }

            request.Status = status;
            WriteRequests(requests);
            return true;
        }
    }

    /// <summary>
    /// Deletes an account request from the store.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <returns><c>true</c> when the request was found and removed; otherwise, <c>false</c>.</returns>
    public bool Delete(Guid id)
    {
        lock (_syncRoot)
        {
            var requests = ReadRequests();
            var removed = requests.RemoveAll(item => item.Id == id) > 0;
            if (removed)
            {
                WriteRequests(requests);
            }

            return removed;
        }
    }

    private static string GetPluginDataPath()
    {
        return Plugin.Instance?.DataFolderPath
            ?? throw new InvalidOperationException("The Account Request plugin instance is not available.");
    }

    private List<AccountRequestModel> ReadRequests()
    {
        if (!File.Exists(_requestsFilePath))
        {
            return [];
        }

        using var stream = File.OpenRead(_requestsFilePath);
        return JsonSerializer.Deserialize<List<AccountRequestModel>>(stream, SerializerOptions) ?? [];
    }

    private void WriteRequests(List<AccountRequestModel> requests)
    {
        var tempFilePath = _requestsFilePath + ".tmp";
        using (var stream = File.Create(tempFilePath))
        {
            JsonSerializer.Serialize(stream, requests, SerializerOptions);
        }

        File.Move(tempFilePath, _requestsFilePath, true);
    }
}
