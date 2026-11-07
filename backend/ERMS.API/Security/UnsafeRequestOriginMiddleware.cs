namespace ERMS.API.Security;

public sealed class UnsafeRequestOriginMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options
    };

    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;

    public UnsafeRequestOriginMiddleware(RequestDelegate next, string[] allowedOrigins)
    {
        _next = next;
        _allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var allowedOrigin in allowedOrigins)
        {
            if (!TryNormalizeOrigin(allowedOrigin, requireOriginOnly: true, out var normalizedOrigin))
            {
                throw new InvalidOperationException($"Invalid configured CORS origin: '{allowedOrigin}'.");
            }

            _allowedOrigins.Add(normalizedOrigin);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method) && !IsWebSocketUpgrade(context.Request))
        {
            await _next(context);
            return;
        }

        var originValues = context.Request.Headers["Origin"];
        string? requestOrigin;

        if (originValues.Count > 0)
        {
            if (originValues.Count != 1 ||
                !TryNormalizeOrigin(originValues[0]!, requireOriginOnly: true, out requestOrigin))
            {
                await RejectRequestAsync(context);
                return;
            }
        }
        else
        {
            var refererValues = context.Request.Headers.Referer;
            if (refererValues.Count != 1 ||
                !TryNormalizeOrigin(refererValues[0]!, requireOriginOnly: false, out requestOrigin))
            {
                await RejectRequestAsync(context);
                return;
            }
        }

        if (!_allowedOrigins.Contains(requestOrigin))
        {
            await RejectRequestAsync(context);
            return;
        }

        await _next(context);
    }

    private static Task RejectRequestAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "Request origin is not allowed."
        });
    }

    private static bool IsWebSocketUpgrade(HttpRequest request)
    {
        return string.Equals(request.Headers["Upgrade"], "websocket", StringComparison.OrdinalIgnoreCase) &&
            request.Headers["Connection"].ToString()
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Contains("upgrade", StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryNormalizeOrigin(string value, bool requireOriginOnly, out string normalizedOrigin)
    {
        normalizedOrigin = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            (requireOriginOnly && (uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))))
        {
            return false;
        }

        normalizedOrigin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return true;
    }
}