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

                if (context.Response.StatusCode != StatusCodes.Status200OK
                    || buffer.Length == 0
                    || context.Response.Headers.ContentEncoding.Count > 0)
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
                    var outBytes = Encoding.UTF8.GetBytes(body);
                    context.Response.ContentLength = outBytes.Length;
                    await originalBody.WriteAsync(outBytes, context.RequestAborted).ConfigureAwait(false);
                    return;
                }

                var pathBase = context.Request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
                var scriptUrl = $"{pathBase}/AccountRequest/login-inject.js";
                var snippet = $"<script src=\"{scriptUrl}\" defer></script>";
                var updated = body.Replace(BodyEndTag, snippet + BodyEndTag, StringComparison.OrdinalIgnoreCase);
                var bytes = Encoding.UTF8.GetBytes(updated);
                context.Response.ContentLength = bytes.Length;
                await originalBody.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
            });

            next(app);
        };

    private static bool ShouldTryInject(PathString path)
    {
        var p = path.Value ?? string.Empty;
        return p.EndsWith("/web/index.html", StringComparison.OrdinalIgnoreCase)
               || p.EndsWith("/web/", StringComparison.OrdinalIgnoreCase)
               || p.Equals("/web", StringComparison.OrdinalIgnoreCase);
    }
}
