using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.AccountRequest;

/// <summary>
/// Injects the login-page account-request script into the Jellyfin web <c>index.html</c> shell
/// so unauthenticated users load it (plugin configuration pages are not loaded on the login view).
/// </summary>
public sealed class LoginInjectStartupFilter : IStartupFilter
{
    private const string BodyEndTag = "</body>";

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (!HttpMethods.IsGet(context.Request.Method) || !ShouldTryInject(context.Request.Path))
                {
                    await nextMiddleware().ConfigureAwait(false);
                    return;
                }

                // Jellyfin enables response compression when the client sends Accept-Encoding.
                // A compressed body in our buffer would skip injection or corrupt the response; force plaintext HTML.
                context.Request.Headers.Remove("Accept-Encoding");

                var originalBody = context.Response.Body;
                await using var buffer = new MemoryStream();
                context.Response.Body = buffer;

                try
                {
                    await nextMiddleware().ConfigureAwait(false);
                }
                finally
                {
                    context.Response.Body = originalBody;
                }

                if (context.Response.StatusCode != StatusCodes.Status200OK || buffer.Length == 0)
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
                    return;
                }

                var contentType = context.Response.ContentType ?? string.Empty;
                if (!contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
                    return;
                }

                buffer.Position = 0;
                using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);

                if (body.IndexOf(BodyEndTag, StringComparison.OrdinalIgnoreCase) < 0
                    || body.Contains("login-inject.js", StringComparison.Ordinal))
                {
                    var passthrough = Encoding.UTF8.GetBytes(body);
                    context.Response.Headers.Remove("Content-Length");
                    context.Response.ContentLength = passthrough.Length;
                    await originalBody.WriteAsync(passthrough, context.RequestAborted).ConfigureAwait(false);
                    return;
                }

                // PathBase is often empty on the outer pipeline; Jellyfin mounts the app under Map(BaseUrl),
                // so the URL prefix (e.g. /jellyfin) appears only on Path.
                var pathPrefix = GetPathPrefixBeforeWeb(context.Request.Path);
                var scriptUrl = string.IsNullOrEmpty(pathPrefix)
                    ? "/AccountRequest/login-inject.js"
                    : $"{pathPrefix}/AccountRequest/login-inject.js";
                var snippet = $"<script src=\"{scriptUrl}\" defer></script>";
                var updated = body.Replace(BodyEndTag, snippet + BodyEndTag, StringComparison.OrdinalIgnoreCase);
                var bytes = Encoding.UTF8.GetBytes(updated);

                context.Response.Headers.Remove("Content-Encoding");
                context.Response.Headers.Remove("Content-Length");
                context.Response.ContentLength = bytes.Length;
                await originalBody.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
            });

            next(app);
        };

    private static bool ShouldTryInject(PathString path)
    {
        var p = path.Value ?? string.Empty;
        if (string.IsNullOrEmpty(p))
        {
            return false;
        }

        return p.Contains("/web/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/web/index.html", StringComparison.OrdinalIgnoreCase)
               || p.EndsWith("/web", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPathPrefixBeforeWeb(PathString path)
    {
        var p = path.Value ?? string.Empty;
        var idx = p.IndexOf("/web", StringComparison.OrdinalIgnoreCase);
        if (idx <= 0)
        {
            return string.Empty;
        }

        return p[..idx].TrimEnd('/');
    }
}
