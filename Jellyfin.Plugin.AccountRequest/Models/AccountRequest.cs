namespace Jellyfin.Plugin.AccountRequest.Models;

/// <summary>
/// Represents a pending, approved, or rejected account request.
/// </summary>
public class AccountRequest
{
    /// <summary>
    /// Gets or sets the unique request identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the requested Jellyfin username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requester's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional message from the requester.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the request was created.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the request status. Valid values are Pending, Approved, and Rejected.
    /// </summary>
    public string Status { get; set; } = "Pending";
}
