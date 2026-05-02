# Changelog

## 1.0.2 - 2026-05-02

- Fix login injection when Jellyfin uses a path Base URL (correct `/Base/AccountRequest/login-inject.js`).
- Strip `Accept-Encoding` for `index.html` responses so HTML is not gzip-compressed before injection (compression previously skipped or broke the script tag).

## 1.0.1 - 2026-05-02

- Load account request UI on the public sign-in page by injecting `login-inject.js` into the web app shell (plugin configuration pages are not loaded before login).
- Place the **Account Request** button correctly for both manual and visual (user picker) login layouts.

## 1.0.0 - 2026-05-02

- Initial release of Account Request Plugin.
- Added public account request submission endpoint.
- Added JSON-backed request storage in the plugin data directory.
- Added admin dashboard page for approving and rejecting requests.
- Added Jellyfin user creation with generated temporary passwords.
- Added release build scripts, plugin metadata, repository manifest, and CI workflow.
