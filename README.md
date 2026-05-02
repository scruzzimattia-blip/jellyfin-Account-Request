# Account Request Plugin

Account Request Plugin lets visitors request a Jellyfin account from the login page and lets administrators review, approve, or reject those requests from the Jellyfin dashboard.

## Features

- Adds an Account Request button and modal for username, email, and message submission.
- Stores requests locally as JSON in the plugin data directory.
- Adds an Account Requests admin dashboard page under the server menu.
- Lets admins approve or reject pending requests.
- Creates approved Jellyfin users and returns a generated temporary password.

## Compatibility

- Jellyfin target ABI: `10.11.0.0`
- Target framework: `.NET 9`
- No external runtime dependencies beyond Jellyfin and ASP.NET Core shared framework assemblies.

## Installation

### Manual Install

1. Download the release zip for your Jellyfin version.
2. Extract it into a plugin folder under your Jellyfin plugins directory, for example `plugins/Account Request_1.0.0.0/`.
3. Ensure the folder contains `Jellyfin.Plugin.AccountRequest.dll`, `Jellyfin.Plugin.AccountRequest.deps.json`, and `meta.json`.
4. Restart Jellyfin.
5. Open the dashboard and confirm Account Request appears in the server menu.

### Plugin Repository

Add this repository URL in Jellyfin under Dashboard > Plugins > Repositories:

```text
https://github.com/mattia/jellyfin-plugin-accountrequest/raw/main/manifest.json
```

The release workflow publishes a zip and an updated release manifest with the computed checksum for tagged releases.

## Configuration Notes

This plugin currently has no required settings. Requests are saved to `requests.json` in the plugin data directory managed by Jellyfin.

Admins should share the generated temporary password with the requester after approving an account. Users can then change their password from Jellyfin after signing in.

## Screenshots

Screenshots will be added after the first packaged release:

- Login page account request button
- Account request modal
- Admin account requests dashboard
