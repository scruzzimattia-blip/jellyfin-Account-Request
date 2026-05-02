using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Security.Cryptography;
using Jellyfin.Plugin.AccountRequest.Services;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AccountRequestModel = Jellyfin.Plugin.AccountRequest.Models.AccountRequest;

namespace Jellyfin.Plugin.AccountRequest.Api;

/// <summary>
/// REST API for submitting and managing Jellyfin account requests.
/// </summary>
[ApiController]
[Route("AccountRequest")]
[Produces("application/json")]
public class AccountRequestController : ControllerBase
{
    private const int TemporaryPasswordBytes = 18;
    private readonly RequestStore _requestStore;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRequestController"/> class.
    /// </summary>
    /// <param name="requestStore">The JSON-backed request store.</param>
    /// <param name="userManager">The Jellyfin user manager.</param>
    public AccountRequestController(RequestStore requestStore, IUserManager userManager)
    {
        _requestStore = requestStore;
        _userManager = userManager;
    }

    /// <summary>
    /// Submits a new account request.
    /// </summary>
    /// <param name="request">The account request payload.</param>
    /// <returns>The stored account request.</returns>
    [HttpPost("submit")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AccountRequestModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AccountRequestModel> Submit([FromBody, Required] SubmitAccountRequest request)
    {
        var validationError = ValidateSubmitRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new ErrorResponse(validationError));
        }

        var accountRequest = new AccountRequestModel
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            Message = request.Message.Trim(),
            RequestedAt = DateTime.UtcNow,
            Status = RequestStatuses.Pending
        };

        return Ok(_requestStore.Add(accountRequest));
    }

    /// <summary>
    /// Lists all account requests.
    /// </summary>
    /// <returns>All account requests.</returns>
    [HttpGet("list")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(typeof(IReadOnlyList<AccountRequestModel>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AccountRequestModel>> List()
    {
        return Ok(_requestStore.GetAll());
    }

    /// <summary>
    /// Approves an account request and creates the Jellyfin user account.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <returns>The generated temporary password for the new user.</returns>
    [HttpPost("approve/{id:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(typeof(ApproveAccountRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApproveAccountRequestResponse>> Approve([FromRoute] Guid id)
    {
        var accountRequest = _requestStore.GetAll().FirstOrDefault(request => request.Id == id);
        if (accountRequest is null)
        {
            return NotFound(new ErrorResponse("Account request was not found."));
        }

        if (!string.Equals(accountRequest.Status, RequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ErrorResponse("Only pending account requests can be approved."));
        }

        if (_userManager.GetUserByName(accountRequest.Username) is not null)
        {
            return Conflict(new ErrorResponse("A Jellyfin user with the requested username already exists."));
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var user = await _userManager.CreateUserAsync(accountRequest.Username).ConfigureAwait(false);
        await _userManager.ChangePassword(user, temporaryPassword).ConfigureAwait(false);

        _requestStore.UpdateStatus(id, RequestStatuses.Approved);
        return Ok(new ApproveAccountRequestResponse(accountRequest.Username, temporaryPassword));
    }

    /// <summary>
    /// Rejects an account request.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <returns>No content when the request is rejected.</returns>
    [HttpPost("reject/{id:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Reject([FromRoute] Guid id)
    {
        if (!_requestStore.UpdateStatus(id, RequestStatuses.Rejected))
        {
            return NotFound(new ErrorResponse("Account request was not found."));
        }

        return NoContent();
    }

    private static string? ValidateSubmitRequest(SubmitAccountRequest request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return "Username is required.";
        }

        if (request.Username.Trim().Length > 64)
        {
            return "Username must be 64 characters or fewer.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required.";
        }

        if (!IsValidEmail(request.Email))
        {
            return "Email must be a valid email address.";
        }

        if (request.Message.Trim().Length > 1000)
        {
            return "Message must be 1000 characters or fewer.";
        }

        return null;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string GenerateTemporaryPassword()
    {
        Span<byte> bytes = stackalloc byte[TemporaryPasswordBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static class RequestStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }
}

/// <summary>
/// Request body for public account request submissions.
/// </summary>
/// <param name="Username">Requested Jellyfin username.</param>
/// <param name="Email">Requester email address.</param>
/// <param name="Message">Optional requester message.</param>
public sealed record SubmitAccountRequest(string Username, string Email, string Message);

/// <summary>
/// Response body returned after approving an account request.
/// </summary>
/// <param name="Username">Created Jellyfin username.</param>
/// <param name="TemporaryPassword">Generated temporary password for the new user.</param>
public sealed record ApproveAccountRequestResponse(string Username, string TemporaryPassword);

/// <summary>
/// Standard error response body.
/// </summary>
/// <param name="Message">Error message.</param>
public sealed record ErrorResponse(string Message);
